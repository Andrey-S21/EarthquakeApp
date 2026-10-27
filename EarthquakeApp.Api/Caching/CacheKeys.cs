namespace EarthquakeApp.Api.Caching;

public static class CacheKeys
{
    public static string Country(string name) => $"country:{Normalize(name)}";
    public static string Earthquakes(string country) => $"earthquakes:{Normalize(country)}";

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
