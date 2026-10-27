using System.Text.Json.Serialization;

namespace EarthquakeApp.Dtos.External;

public class UsgsResponse
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("metadata")] public UsgsMetadata? Metadata { get; set; }
    [JsonPropertyName("features")] public List<UsgsFeature> Features { get; set; } = new();
}

public class UsgsMetadata
{
    [JsonPropertyName("generated")] public long Generated { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("api")] public string? Api { get; set; }
    [JsonPropertyName("count")] public int Count { get; set; }
}

public class UsgsFeature
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("properties")] public UsgsProperties Properties { get; set; } = new();
    [JsonPropertyName("geometry")] public UsgsGeometry Geometry { get; set; } = new();
}

public class UsgsProperties
{
    [JsonPropertyName("mag")] public double? Mag { get; set; }
    [JsonPropertyName("place")] public string? Place { get; set; }
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("updated")] public long Updated { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("detail")] public string? Detail { get; set; }
    [JsonPropertyName("felt")] public int? Felt { get; set; }
    [JsonPropertyName("alert")] public string? Alert { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("tsunami")] public int Tsunami { get; set; }
    [JsonPropertyName("sig")] public int? Sig { get; set; }
    [JsonPropertyName("net")] public string? Net { get; set; }
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("magType")] public string? MagType { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
}

public class UsgsGeometry
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    // [longitud, latitud, profundidad en km]
    [JsonPropertyName("coordinates")] public List<double> Coordinates { get; set; } = new();
}