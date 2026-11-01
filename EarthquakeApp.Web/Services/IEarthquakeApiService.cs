using EarthquakeApp.Dtos;

namespace EarthquakeApp.Web.Services;

public interface IEarthquakeApiService
{
    Task<List<EarthquakeDto>> GetAllEarthquakesAsync();

    Task<SearchResponseDto> SearchByCountryAsync(string country);
}
