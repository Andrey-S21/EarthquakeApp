using System.Text.Json.Serialization;

namespace EarthquakeApp.Dtos.External;

// Modelo de un país según el formato de RestCountries v5
public class RestCountryResponse
{
    [JsonPropertyName("names")] public RestCountryName Name { get; set; } = new();
    [JsonPropertyName("codes")] public RestCountryCodes? Codes { get; set; }
    [JsonPropertyName("capitals")] public List<RestCountryCapital>? Capitals { get; set; }
    [JsonPropertyName("region")] public string? Region { get; set; }
    [JsonPropertyName("subregion")] public string? Subregion { get; set; }
    [JsonPropertyName("population")] public long Population { get; set; }
    [JsonPropertyName("flag")] public RestCountryFlag? Flag { get; set; }
    [JsonPropertyName("languages")] public List<RestCountryLanguage>? Languages { get; set; }
    [JsonPropertyName("currencies")] public List<RestCountryCurrency>? Currencies { get; set; }

    // Atajos para mantener compatibilidad con el resto del código
    [JsonIgnore] public string? Cca2 => Codes?.Alpha2;
    [JsonIgnore] public string? Cca3 => Codes?.Alpha3;
}

public class RestCountryName
{
    [JsonPropertyName("common")] public string Common { get; set; } = string.Empty;
    [JsonPropertyName("official")] public string? Official { get; set; }
}

public class RestCountryCodes
{
    [JsonPropertyName("alpha_2")] public string? Alpha2 { get; set; }
    [JsonPropertyName("alpha_3")] public string? Alpha3 { get; set; }
}

public class RestCountryCapital
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class RestCountryFlag
{
    [JsonPropertyName("url_png")] public string? UrlPng { get; set; }
    [JsonPropertyName("url_svg")] public string? UrlSvg { get; set; }
    [JsonPropertyName("emoji")] public string? Emoji { get; set; }
}

public class RestCountryLanguage
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class RestCountryCurrency
{
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("symbol")] public string? Symbol { get; set; }
}