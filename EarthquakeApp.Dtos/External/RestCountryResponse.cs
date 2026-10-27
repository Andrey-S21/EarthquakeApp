using System.Text.Json.Serialization;

namespace EarthquakeApp.Dtos.External;

public class RestCountryResponse
{
    [JsonPropertyName("name")] public RestCountryName Name { get; set; } = new();
    [JsonPropertyName("cca2")] public string? Cca2 { get; set; }
    [JsonPropertyName("cca3")] public string? Cca3 { get; set; }
    [JsonPropertyName("capital")] public List<string>? Capital { get; set; }
    [JsonPropertyName("region")] public string? Region { get; set; }
    [JsonPropertyName("subregion")] public string? Subregion { get; set; }
    [JsonPropertyName("population")] public long Population { get; set; }
    [JsonPropertyName("flags")] public RestCountryFlags? Flags { get; set; }
    // clave = código ("spa"), valor = nombre ("Spanish")
    [JsonPropertyName("languages")] public Dictionary<string, string>? Languages { get; set; }
    // clave = código ("CRC"), valor = datos de la moneda
    [JsonPropertyName("currencies")] public Dictionary<string, RestCountryCurrency>? Currencies { get; set; }
}

public class RestCountryName
{
    [JsonPropertyName("common")] public string Common { get; set; } = string.Empty;
    [JsonPropertyName("official")] public string? Official { get; set; }
}

public class RestCountryFlags
{
    [JsonPropertyName("png")] public string? Png { get; set; }
    [JsonPropertyName("svg")] public string? Svg { get; set; }
    [JsonPropertyName("alt")] public string? Alt { get; set; }
}

public class RestCountryCurrency
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("symbol")] public string? Symbol { get; set; }
}