using EarthquakeApp.Api.Caching;
using EarthquakeApp.Api.Configuration;
using EarthquakeApp.Api.Exceptions;
using EarthquakeApp.Data.Models;
using EarthquakeApp.Data.Repositories;
using EarthquakeApp.Dtos;
using EarthquakeApp.Dtos.External;
using EarthquakeApp.Dtos.Mappers;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EarthquakeApp.Api.Services
{
    // Esta es la implementación real del servicio de terremotos
    // Aquí es donde se obtienen datos, se guardan, se cachean, etc.
    public class EarthquakeService : IEarthquakeService
    {
        // Estas son las herramientas necesarias para trabajar
        private readonly HttpClient _httpClient;                    // Se utiliza para hacer peticiones a la API USGS
        private readonly IEarthquakeRepository _repository;         // Se utiliza para guardar/consultar en la base de datos
        private readonly ICacheService _cacheService;               // Se utiliza para guardar datos en memoria (caché)
        private readonly IOptions<UsgsOptions> _usgsOptions;        // Se almacena la configuración de USGS (URLs, fechas, etc.)
        private readonly IOptions<CacheOptions> _cacheOptions;      // Se almacena la configuración del caché (tiempo de expiración)
        private readonly ILogger<EarthquakeService> _logger;        // Se utiliza para escribir registros/logs

        // Constructor: aquí se reciben todas las herramientas (inyección de dependencias)
        public EarthquakeService(
            HttpClient httpClient,
            IEarthquakeRepository repository,
            ICacheService cacheService,
            IOptions<UsgsOptions> usgsOptions,
            IOptions<CacheOptions> cacheOptions,
            ILogger<EarthquakeService> logger)
        {
            _httpClient = httpClient;
            _repository = repository;
            _cacheService = cacheService;
            _usgsOptions = usgsOptions;
            _cacheOptions = cacheOptions;
            _logger = logger;
        }

        // Acción principal para listar los terremotos almacenados en SQL
        public async Task<List<EarthquakeDto>> GetAllEarthquakesAsync()
        {
            try
            {
                var earthquakes = await _repository.GetAllAsync();

                var result = earthquakes
                    .OrderByDescending(eq => eq.EventTime)
                    .Select(MapToDto)
                    .ToList();

                _logger.LogInformation("Se obtuvieron {Count} terremotos desde SQL", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado consultando terremotos en SQL");
                throw;
            }
        }

        // Acción principal para filtrar terremotos por país usando registros en SQL
        public async Task<List<EarthquakeDto>> GetEarthquakesByCountryAsync(string countryName)
        {
            // Paso 1: Se valida que el nombre del país no esté vacío o sea inválido
            if (string.IsNullOrWhiteSpace(countryName))
            {
                throw new BadRequestException("El nombre del país no puede estar vacío.");
            }

            // Paso 2: Se limpia el nombre del país (se eliminan espacios extras, se convierte a minúsculas)
            var normalizedCountryName = countryName.Trim().ToLowerInvariant();

            // Paso 3: Se crea una clave única para el caché
            var cacheKey = CacheKeys.Earthquakes(normalizedCountryName);

            _logger.LogInformation("Buscando terremotos para el país: {Country}", normalizedCountryName);

            try
            {
                // Paso 4: Se consulta caché para evitar filtrar SQL repetidamente
                var cachedEarthquakes = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await FetchEarthquakesFromDatabaseAsync(normalizedCountryName),
                    TimeSpan.FromMinutes(_cacheOptions.Value.EarthquakeMinutes)
                );

                _logger.LogInformation("Se obtuvieron {Count} terremotos para {Country}",
                    cachedEarthquakes.Count, normalizedCountryName);

                return cachedEarthquakes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo terremotos por país desde SQL");
                throw;
            }
        }

        // Acción secundaria: filtra en memoria usando los registros ya persistidos en SQL
        private async Task<List<EarthquakeDto>> FetchEarthquakesFromDatabaseAsync(string countryName)
        {
            var earthquakes = await GetAllEarthquakesAsync();

            var filteredEarthquakes = earthquakes
                .Where(eq => !string.IsNullOrWhiteSpace(eq.Place) &&
                             eq.Place.Contains(countryName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Se filtraron {Filtered} terremotos para el país: {Country}",
                filteredEarthquakes.Count, countryName);

            return filteredEarthquakes;
        }

        // Nota: la descarga desde USGS ahora se realiza al arrancar la API en UsgsSyncHostedService.

        private static EarthquakeDto MapToDto(Earthquake earthquake)
        {
            return new EarthquakeDto
            {
                Id = earthquake.Id,
                Magnitude = earthquake.Magnitude,
                Place = earthquake.Place ?? string.Empty,
                Time = earthquake.EventTime ?? DateTime.MinValue,
                Latitude = earthquake.Latitude ?? 0,
                Longitude = earthquake.Longitude ?? 0,
                DepthKm = earthquake.Depth,
                Url = earthquake.Url
            };
        }
    }
}
