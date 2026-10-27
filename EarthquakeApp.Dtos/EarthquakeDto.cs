using System;
using System.Collections.Generic;
using System.Text;

namespace EarthquakeApp.Dtos;

public class EarthquakeDto
{
    public string Id { get; set; } = string.Empty;
    public double? Magnitude { get; set; }
    public string? MagnitudeType { get; set; }
    public string Place { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DepthKm { get; set; }
    public bool Tsunami { get; set; }
    public string? Url { get; set; }
}
