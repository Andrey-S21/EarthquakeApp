using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EarthquakeApp.Dtos;
using EarthquakeApp.Web.Configuration;
using Microsoft.Extensions.Options;

namespace EarthquakeApp.Web.Services;

public class EarthquakeApiService : IEarthquakeApiService
{
    private readonly HttpClient _httpClient;
    private readonly ApiOptions _apiOptions;

    public EarthquakeApiService(HttpClient httpClient, IOptions<ApiOptions> apiOptions)
    {
        _httpClient = httpClient;
        _apiOptions = apiOptions.Value;
    }

    // Acción principal para cargar todos los terremotos disponibles desde la Web API
    public async Task<List<EarthquakeDto>> GetAllEarthquakesAsync()
    {
        var response = await _httpClient.GetAsync(BuildApiUrl("api/earthquakes"));

        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiExceptionAsync(response, "No se pudo cargar la lista de terremotos.");
        }

        var result = await response.Content.ReadFromJsonAsync<List<EarthquakeDto>>();
        return result ?? new List<EarthquakeDto>();
    }

    // Acción principal para buscar terremotos por país y combinar respuesta de país
    public async Task<SearchResponseDto> SearchByCountryAsync(string country)
    {
        var countryQuery = Uri.EscapeDataString(country);
        var response = await _httpClient.GetAsync(BuildApiUrl($"api/earthquakes/search?country={countryQuery}"));

        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiExceptionAsync(response, "No fue posible realizar la búsqueda por país.");
        }

        var result = await response.Content.ReadFromJsonAsync<SearchResponseDto>();
        return result ?? new SearchResponseDto();
    }

    private string BuildApiUrl(string endpoint)
    {
        var baseUrl = _apiOptions.BaseUrl?.TrimEnd('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("No se configuró la URL base de la API.");
        }

        return $"{baseUrl}/{endpoint.TrimStart('/')}";
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response, string fallbackMessage)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                throw new InvalidOperationException(error.Message);
            }
        }
        catch (JsonException)
        {
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("No se encontraron resultados para la búsqueda solicitada.");
        }

        throw new InvalidOperationException(fallbackMessage);
    }
}
