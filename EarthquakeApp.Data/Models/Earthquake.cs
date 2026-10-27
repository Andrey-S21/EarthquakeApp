using System;
using System.Collections.Generic;

namespace EarthquakeApp.Data.Models;

public partial class Earthquake
{
    public string Id { get; set; } = null!;

    public double? Magnitude { get; set; }

    public string? Place { get; set; }

    public DateTime? EventTime { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? Depth { get; set; }

    public string? Url { get; set; }
}
