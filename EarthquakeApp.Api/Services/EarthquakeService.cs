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

        // Este método original se conserva para mantener la lógica existente de carga/sincronización desde USGS
        // Puede seguir utilizándose por procesos de refresco sin impactar el flujo del frontend SQL-first.
        private async Task<List<EarthquakeDto>> FetchEarthquakesAsync(string countryName)
        {
            var options = _usgsOptions.Value;

            // Paso 1: Se construye la URL de la petición a USGS con los parámetros
            var uriBuilder = new UriBuilder(options.BaseUrl)
            {
                Query = $"format={options.Format}&starttime={options.StartTime}&endtime={options.EndTime}&minmagnitude={options.MinMagnitude}"
            };

            var url = uriBuilder.ToString();

            _logger.LogInformation("Llamando a USGS API: {Url}", url);

            // Paso 2: Se hace la petición GET a la API USGS
            var response = await _httpClient.GetAsync(url);

            // Paso 3: Se verifica que la respuesta sea exitosa (código 200)
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    $"La API USGS respondió con error: {response.StatusCode}");
            }

            // Paso 4: Se lee el contenido JSON de la respuesta
            var content = await response.Content.ReadAsStringAsync();

            // Paso 5: Se convierte el JSON a un objeto UsgsResponse que se entiende
            var optionsJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var usgsResponse = JsonSerializer.Deserialize<UsgsResponse>(content, optionsJson);

            if (usgsResponse?.Features == null || usgsResponse.Features.Count == 0)
            {
                _logger.LogWarning("No se encontraron terremotos en la respuesta de USGS");
                return new List<EarthquakeDto>();
            }

            // Paso 6: Se convierte cada terremoto de USGS a nuestro formato EarthquakeDto
            var earthquakes = usgsResponse.Features
                .Select(feature => feature.ToDto())
                .ToList();

            // Paso 6.5: Se filtra la lista por país
            var filteredEarthquakes = earthquakes
                .Where(eq => eq.Place != null && eq.Place.Contains(countryName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Se filtraron {Original} terremotos a {Filtered} para el país: {Country}",
                earthquakes.Count, filteredEarthquakes.Count, countryName);

            // Paso 7: Se guarda cada terremoto en la base de datos (si no existe ya)
            foreach (var earthquake in filteredEarthquakes)
            {
                try
                {
                    var existingEarthquake = await _repository.GetByIdAsync(earthquake.Id);

                    if (existingEarthquake == null)
                    {
                        var newEarthquake = new Earthquake
                        {
                            Id = earthquake.Id,
                            Magnitude = earthquake.Magnitude,
                            Place = earthquake.Place,
                            EventTime = earthquake.Time,
                            Latitude = earthquake.Latitude,
                            Longitude = earthquake.Longitude,
                            Depth = earthquake.DepthKm,
                            Url = earthquake.Url
                        };

                        await _repository.AddAsync(newEarthquake);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando el terremoto {EarthquakeId}", earthquake.Id);
                }
            }

            return filteredEarthquakes;
        }

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
