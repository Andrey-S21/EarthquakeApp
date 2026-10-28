using EarthquakeApp.Dtos;

namespace EarthquakeApp.Web.Models;

public class HomeIndexViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string? UserMessage { get; set; }
    public CountryDto? CountryInfo { get; set; }
    public bool IsCountryInfoMinimized { get; set; }
    public List<EarthquakeDto> Earthquakes { get; set; } = new();
}
