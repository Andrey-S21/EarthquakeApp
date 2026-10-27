using EarthquakeApp.Dtos.External;

namespace EarthquakeApp.Dtos.Mappers;

public static class ExternalMappingExtensions
{
    public static EarthquakeDto ToDto(this UsgsFeature feature)
    {
        // coordinates = [longitud, latitud, profundidad en km]
        var coords = feature.Geometry.Coordinates;

        return new EarthquakeDto
        {
            Id = feature.Id,
            Magnitude = feature.Properties.Mag,
            MagnitudeType = feature.Properties.MagType,
            Place = feature.Properties.Place ?? string.Empty,
            Title = feature.Properties.Title,
            Time = DateTimeOffset.FromUnixTimeMilliseconds(feature.Properties.Time).UtcDateTime,
            Longitude = coords.Count > 0 ? coords[0] : 0,
            Latitude = coords.Count > 1 ? coords[1] : 0,
            DepthKm = coords.Count > 2 ? coords[2] : null,
            Tsunami = feature.Properties.Tsunami == 1,
            Url = feature.Properties.Url
        };
    }

    public static CountryDto ToDto(this RestCountryResponse country)
    {
        return new CountryDto
        {
            Name = country.Name.Common,
            OfficialName = country.Name.Official,
            Capital = country.Capital?.FirstOrDefault(),
            Region = country.Region,
            Subregion = country.Subregion,
            Population = country.Population,
            FlagUrl = country.Flags?.Png ?? country.Flags?.Svg,
            Languages = country.Languages?.Values.ToList() ?? new List<string>(),
            Currencies = country.Currencies?.Values
                .Select(c => string.IsNullOrWhiteSpace(c.Symbol) ? c.Name ?? "" : $"{c.Name} ({c.Symbol})")
                .Where(c => c.Length > 0)
                .ToList() ?? new List<string>()
        };
    }
}