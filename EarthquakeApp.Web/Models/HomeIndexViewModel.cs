using EarthquakeApp.Dtos;

namespace EarthquakeApp.Web.Models;

public class HomeIndexViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string? UserMessage { get; set; }
    public CountryDto? CountryInfo { get; set; }
    public bool IsCountryInfoMinimized { get; set; }
    public List<EarthquakeDto> Earthquakes { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalItems { get; set; }
    public int TotalPages => TotalItems <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
}
