using System.Globalization;
using System.Text.Json;
using EarthquakeApp.Api.Configuration;
using EarthquakeApp.Data.Models;
using EarthquakeApp.Data.Repositories;
using EarthquakeApp.Dtos.External;
using EarthquakeApp.Dtos.Mappers;
using Microsoft.Extensions.Options;

namespace EarthquakeApp.Api.Services
{
    // Este servicio se ejecuta automáticamente cuando la API arranca.
    // Su trabajo es descargar los terremotos de USGS y guardarlos en la base de datos.
    // - Si la base está vacía, se descargan todos (desde StartTime hasta EndTime).
    // - Si ya hay datos, solo se piden los terremotos nuevos (posteriores al último guardado).
    public class UsgsSyncHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<UsgsOptions> _usgsOptions;
        private readonly ILogger<UsgsSyncHostedService> _logger;

        // Se reciben las herramientas necesarias para trabajar
        public UsgsSyncHostedService(
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            IOptions<UsgsOptions> usgsOptions,
            ILogger<UsgsSyncHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _usgsOptions = usgsOptions;
            _logger = logger;
        }

        // Este es el método que se ejecuta al arrancar la API
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Se crea un "espacio de trabajo" para poder usar la base de datos desde aquí
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IEarthquakeRepository>();

                var options = _usgsOptions.Value;

                // Se leen las fechas configuradas en appsettings.json
                var configuredStart = DateTime.Parse(options.StartTime, CultureInfo.InvariantCulture);
                var configuredEnd = DateTime.Parse(options.EndTime, CultureInfo.InvariantCulture);

                // Se evita pedir fechas futuras, porque todavía no existen terremotos ahí
                var endDate = configuredEnd > DateTime.UtcNow ? DateTime.UtcNow : configuredEnd;

                // Se revisa si ya hay terremotos guardados
                var latest = await repository.GetLatestEventTimeAsync();

                DateTime startDate;
                if (latest == null)
                {
                    // La base está vacía: se descarga todo desde el inicio
                    startDate = configuredStart;
                    _logger.LogInformation("Base de datos vacía. Se inicia la carga completa desde {Start:yyyy-MM-dd}.", startDate);
                }
                else
                {
                    // Ya hay datos: solo se piden los terremotos posteriores al último guardado
                    startDate = latest.Value;
                    _logger.LogInformation("Ya existen datos. Solo se buscarán terremotos nuevos desde {Start:yyyy-MM-dd HH:mm}.", startDate);
                }

                // Si no hay nada nuevo que pedir, no se llama a la API
                if (startDate >= endDate)
                {
                    _logger.LogInformation("No hay rango nuevo que consultar. No se llama a USGS.");
                    return;
                }

                var totalSaved = 0;

                // Se descarga mes por mes, porque USGS no permite más de 20.000 resultados por llamada
                var currentStart = startDate;
                while (currentStart < endDate && !stoppingToken.IsCancellationRequested)
                {
                    var currentEnd = currentStart.AddMonths(1);
                    if (currentEnd > endDate)
                    {
                        currentEnd = endDate;
                    }

                    totalSaved += await DownloadAndSaveRangeAsync(repository, currentStart, currentEnd, stoppingToken);

                    currentStart = currentEnd;
                }

                _logger.LogInformation("Sincronización con USGS terminada. Se guardaron {Total} terremotos nuevos.", totalSaved);
            }
            catch (OperationCanceledException)
            {
                // La API se está cerrando, así que se detiene la descarga sin mostrar error
                _logger.LogInformation("La sincronización con USGS fue cancelada porque la API se detuvo.");
            }
            catch (Exception ex)
            {
                // Si algo falla, se registra el error pero la API sigue funcionando
                _logger.LogError(ex, "Error durante la sincronización con USGS.");
            }
        }

        // Se descarga un tramo de fechas desde USGS y se guardan solo los terremotos que no existían
        private async Task<int> DownloadAndSaveRangeAsync(
            IEarthquakeRepository repository,
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken)
        {
            var options = _usgsOptions.Value;
            var httpClient = _httpClientFactory.CreateClient();

            // Se arma la dirección de la consulta con las fechas y la magnitud mínima
            var url = $"{options.BaseUrl}?format={options.Format}" +
                      $"&starttime={start:yyyy-MM-ddTHH:mm:ss}" +
                      $"&endtime={end:yyyy-MM-ddTHH:mm:ss}" +
                      $"&minmagnitude={options.MinMagnitude.ToString(CultureInfo.InvariantCulture)}";

            _logger.LogInformation("Descargando terremotos de USGS: {Start:yyyy-MM-dd} a {End:yyyy-MM-dd}", start, end);

            // Se hace la llamada a USGS
            var response = await httpClient.GetAsync(url, cancellationToken);

            // Si USGS responde con error, se registra y se continúa con el siguiente tramo
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("USGS respondió {Status} para el tramo {Start:yyyy-MM-dd} a {End:yyyy-MM-dd}.",
                    response.StatusCode, start, end);
                return 0;
            }

            // Se lee la respuesta y se convierte a objetos que el programa entiende
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var usgsResponse = JsonSerializer.Deserialize<UsgsResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (usgsResponse?.Features == null || usgsResponse.Features.Count == 0)
            {
                return 0;
            }

            // Se convierten los datos de USGS al formato de nuestra tabla
            var earthquakes = usgsResponse.Features
                .Select(feature => feature.ToDto())
                .Where(dto => !string.IsNullOrWhiteSpace(dto.Id))
                .DistinctBy(dto => dto.Id)
                .Select(dto => new Earthquake
                {
                    Id = dto.Id,
                    Magnitude = dto.Magnitude,
                    Place = dto.Place,
                    EventTime = dto.Time,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Depth = dto.DepthKm,
                    Url = dto.Url
                })
                .ToList();

            // Se revisa cuáles ya estaban guardados para no duplicarlos
            var existingIds = await repository.GetExistingIdsAsync(earthquakes.Select(e => e.Id));
            var newEarthquakes = earthquakes.Where(e => !existingIds.Contains(e.Id)).ToList();

            // Se guardan todos los nuevos de una sola vez
            if (newEarthquakes.Count > 0)
            {
                await repository.AddRangeAsync(newEarthquakes);
            }

            _logger.LogInformation("Tramo {Start:yyyy-MM-dd}: {Received} recibidos, {Saved} nuevos guardados.",
                start, earthquakes.Count, newEarthquakes.Count);

            return newEarthquakes.Count;
        }
    }
}
