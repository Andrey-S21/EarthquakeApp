using System.Text.Json.Serialization;

namespace EarthquakeApp.Dtos.External;

/// <summary>
/// Wrapper para la respuesta de RestCountries v5.
/// La API devuelve: { "data": { "objects": [...], "meta": {...} } }
/// </summary>
public class RestCountriesV5Response
{
    [JsonPropertyName("data")]
    public RestCountriesV5Data? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<RestCountriesV5Error>? Errors { get; set; }
}

public class RestCountriesV5Data
{
    [JsonPropertyName("objects")]
    public List<RestCountryResponse>? Objects { get; set; }

    [JsonPropertyName("meta")]
    public RestCountriesV5Meta? Meta { get; set; }
}

public class RestCountriesV5Meta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("more")]
    public bool More { get; set; }
}

public class RestCountriesV5Error
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
