namespace EarthquakeApp.Api.Configuration;

public class RestCountriesOptions
{
    public const string SectionName = "RestCountries";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class UsgsOptions
{
    public const string SectionName = "Usgs";

    public string BaseUrl { get; set; } = string.Empty;
    public string Format { get; set; } = "geojson";
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public double MinMagnitude { get; set; }
}

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int CountryMinutes { get; set; } = 60;
    public int EarthquakeMinutes { get; set; } = 30;
}