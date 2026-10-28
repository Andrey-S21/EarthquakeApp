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
            // Paso 1: Si ya se cargaron TODOS los países en esta sesión, usar el caché local
            if (_countryCache == null)
            {
                _countryCache = await LoadAllCountriesAsync();
            }

            // Paso 2: Si aún no hay caché (por error), retornar null
            if (_countryCache == null || _countryCache.Count == 0)
            {
                _logger.LogWarning("No se pudo cargar la lista de países desde la API de RestCountries");
                return null;
            }

            // Paso 3: Se busca el país en el caché con búsqueda flexible
            // Se pueden buscar por: nombre común, nombre oficial, código cca2 o cca3
            var country = _countryCache.FirstOrDefault(c =>
                c.Name?.Common?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true ||
                c.Name?.Official?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true ||
                (c.Cca2?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.Cca3?.Equals(countryName, StringComparison.OrdinalIgnoreCase) == true)
            );

            if (country == null)
            {
                _logger.LogWarning("País no encontrado en la API: {Country}", countryName);
                return null;
            }

            // Se usa el mapper que ya existe para convertir el objeto
            var countryDto = country.ToDto();

            _logger.LogInformation("País obtenido y convertido: {CountryName}", countryDto.Name);

            return countryDto;
        }

        private async Task<List<RestCountryResponse>?> LoadAllCountriesAsync()
        {
            var options = _restOptions.Value;

            _logger.LogInformation("Cargando países desde RestCountries API");

            try
            {
                // Se crea la petición con el header de autorización
                var request = new HttpRequestMessage(HttpMethod.Get, options.BaseUrl);

                // Se agrega el API Key en el header si está configurado
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {options.ApiKey}");
                }

                var response = await _httpClient.SendAsync(request);

                // Se verifica que la respuesta sea exitosa
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("RestCountries API respondió con error: {StatusCode}", response.StatusCode);
                    return null;
                }

                // Se lee el contenido JSON de la respuesta
                var content = await response.Content.ReadAsStringAsync();

                // Se convierte el JSON a un arreglo de objetos RestCountryResponse
                var options_json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countries = JsonSerializer.Deserialize<List<RestCountryResponse>>(content, options_json);

                // Se verifica que se obtuvieron resultados
                if (countries != null && countries.Count > 0)
                {
                    _logger.LogInformation("Se cargaron {Count} países desde RestCountries", countries.Count);
                    return countries;
                }

                _logger.LogWarning("RestCountries no contiene países válidos");
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Error deserializando RestCountries API");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogWarning(httpEx, "Error de conexión con RestCountries API");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando países desde la API");
                return null;
            }
        }
    }
}
