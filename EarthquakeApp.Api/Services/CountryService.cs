using EarthquakeApp.Api.Caching;
using EarthquakeApp.Api.Configuration;
using EarthquakeApp.Api.Exceptions;
using EarthquakeApp.Dtos;
using EarthquakeApp.Dtos.External;
using EarthquakeApp.Dtos.Mappers;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EarthquakeApp.Api.Services
{
    // Este servicio consume la API pública de RestCountries para obtener información de países
    // Realiza llamadas HTTP a https://restcountries.com y cachea los resultados
    public class CountryService : ICountryService
    {
        // Estas son las herramientas necesarias para trabajar
        private readonly HttpClient _httpClient;                        // Se utiliza para hacer peticiones a RestCountries API
        private readonly ICacheService _cacheService;                   // Se utiliza para guardar datos en memoria (caché)
        private readonly IOptions<RestCountriesOptions> _restOptions;   // Se almacena la configuración de RestCountries (URL, etc.)
        private readonly ILogger<CountryService> _logger;               // Se utiliza para escribir registros/logs
        private static List<RestCountryResponse>? _countryCache;       // Caché en memoria de todos los países (se carga una sola vez)

        // Constructor: aquí se reciben todas las herramientas (inyección de dependencias)
        public CountryService(
            HttpClient httpClient,
            ICacheService cacheService,
            IOptions<RestCountriesOptions> restOptions,
            ILogger<CountryService> logger)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            _restOptions = restOptions;
            _logger = logger;
        }

        // Este es el método principal: se obtiene información de un país desde la API RestCountries
        public async Task<CountryDto?> GetCountryByNameAsync(string countryName)
        {
            // Paso 1: Se valida que el nombre del país no esté vacío
            if (string.IsNullOrWhiteSpace(countryName))
            {
                throw new BadRequestException("El nombre del país no puede estar vacío.");
            }

            // Paso 2: Se limpia y normaliza el nombre del país
            var normalizedCountryName = countryName.Trim().ToLowerInvariant();

            // Paso 3: Se crea una clave única para el caché
            var cacheKey = CacheKeys.Country(normalizedCountryName);

            _logger.LogInformation("Buscando información del país en RestCountries: {Country}", normalizedCountryName);

            try
            {
                // Paso 4: Se intenta obtener del caché (si ya se pidió, se evita pedir a la API nuevamente)
                var cachedCountry = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await FetchCountryAsync(normalizedCountryName),
                    TimeSpan.FromMinutes(60)  // Se guardan en caché por 60 minutos
                );

                if (cachedCountry != null)
                {
                    _logger.LogInformation("Se obtuvo información del país: {Country}", normalizedCountryName);
                }
                else
                {
                    _logger.LogWarning("No se encontraron datos para el país: {Country}", normalizedCountryName);
                }

                return cachedCountry;
            }
            catch (HttpRequestException ex)
            {
                // Si hay error de conexión con RestCountries API, se retorna null (es tolerante a fallos)
                _logger.LogWarning(ex, "Error conectando a la API de RestCountries. Continuando sin datos del país.");
                return null;
            }
            catch (ExternalServiceException ex)
            {
                // Si hay error procesando la API, se retorna null (es tolerante a fallos)
                _logger.LogWarning(ex, "Error en la API de RestCountries. Continuando sin datos del país.");
                return null;
            }
            catch (Exception ex)
            {
                // Cualquier otro error inesperado (no debe bloquear)
                _logger.LogWarning(ex, "Error inesperado obteniendo información del país. Continuando sin datos.");
                return null;
            }
        }

        // Este método privado realiza el trabajo pesado: se llama a RestCountries y se procesan los datos
        private async Task<CountryDto?> FetchCountryAsync(string countryName)
        {
            // Se construye la URL dinámica con el país buscado (sin autenticación, ya que es pública)
            var url = $"{_restOptions.Value.BaseUrl}?q={Uri.EscapeDataString(countryName)}";

            var countries = await LoadCountriesBySearchAsync(url);

            // Paso 2: Si no se cargaron países, retornar null
            if (countries == null || countries.Count == 0)
            {
                _logger.LogWarning("País no encontrado en RestCountries: {Country}", countryName);
                return null;
            }

            // Paso 3: Se busca el país en los resultados con búsqueda flexible
            // Se pueden buscar por: nombre común, nombre oficial, código cca2 o cca3
            var country = countries.FirstOrDefault(c =>
                c.Name?.Common?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true ||
                c.Name?.Official?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true ||
                (c.Cca2?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.Cca3?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true)
            );

            if (country == null)
            {
                _logger.LogWarning("País no encontrado en la búsqueda: {Country}", countryName);
                return null;
            }

            // Se usa el mapper que ya existe para convertir el objeto
            var countryDto = country.ToDto();

            _logger.LogInformation("País obtenido y convertido: {CountryName}", countryDto.Name);

            return countryDto;
        }

        // Método privado para buscar países por query en RestCountries v5
        private async Task<List<RestCountryResponse>?> LoadCountriesBySearchAsync(string url)
        {
            _logger.LogInformation("Buscando país en RestCountries API v5");

            try
            {
                // Se crea la petición con el header de autorización
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // RestCountries v5 requiere autenticación con Bearer token
                if (!string.IsNullOrEmpty(_restOptions.Value.ApiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {_restOptions.Value.ApiKey}");
                }

                var response = await _httpClient.SendAsync(request);

                // Se verifica que la respuesta sea exitosa
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("RestCountries API respondió con error: {StatusCode}", response.StatusCode);
                    return null;
                }

                _logger.LogInformation("Respuesta exitosa de RestCountries");

                // Se lee el contenido JSON de la respuesta
                var content = await response.Content.ReadAsStringAsync();

                if (content.Length < 500)
                {
                    _logger.LogInformation("JSON recibido: {Content}", content);
                }

                var options_json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // v5 devuelve un wrapper { data: { objects: [...] } }
                var wrapper = JsonSerializer.Deserialize<RestCountriesV5Response>(content, options_json);

                _logger.LogInformation("Wrapper deserializado. Data: {DataNull}, Objects: {ObjectsNull}", 
                    wrapper?.Data == null ? "null" : "not-null",
                    wrapper?.Data?.Objects == null ? "null" : wrapper.Data.Objects.Count.ToString());

                if (wrapper?.Data?.Objects != null && wrapper.Data.Objects.Count > 0)
                {
                    _logger.LogInformation("Se cargaron {Count} paises desde RestCountries v5", wrapper.Data.Objects.Count);
                    return wrapper.Data.Objects;
                }

                _logger.LogWarning("RestCountries no contiene resultados para la búsqueda");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conexión con RestCountries API");
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializando RestCountries API - respuesta no esperada");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado cargando país desde la API");
                return null;
            }
        }
    }
}
