using System;
using System.Collections.Generic;
using System.Text;

namespace EarthquakeApp.Dtos;

public class CountryDto
{
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public string? Subregion { get; set; }
    public long Population { get; set; }
    public string? FlagUrl { get; set; }
    public List<string> Languages { get; set; } = new();
    public List<string> Currencies { get; set; } = new();
}
