using System;
using System.Collections.Generic;
using System.Text;

namespace EarthquakeApp.Dtos;

public class SearchResponseDto
{
    public CountryDto? Country { get; set; }
    public int TotalEarthquakes { get; set; }
    public List<EarthquakeDto> Earthquakes { get; set; } = new();
}