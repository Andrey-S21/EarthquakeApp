using EarthquakeApp.Api.Exceptions;
using EarthquakeApp.Data.Models;
using EarthquakeApp.Data.Repositories;
using EarthquakeApp.Dtos;

namespace EarthquakeApp.Api.Services
{
    // Esta es la implementación real del servicio de terremotos
    // Aquí es donde se obtienen datos, se guardan, se cachean, etc.
    public class EarthquakeService : IEarthquakeService
    {
        // Estas son las herramientas necesarias para trabajar
        private readonly IEarthquakeRepository _repository;         // Se utiliza para consultar la base de datos
        private readonly ILogger<EarthquakeService> _logger;        // Se utiliza para escribir registros/logs

        // Constructor: aquí se reciben todas las herramientas (inyección de dependencias)
        public EarthquakeService(
            IEarthquakeRepository repository,
            ILogger<EarthquakeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // Acción principal para listar los terremotos almacenados en SQL
        public async Task<List<EarthquakeDto>> GetAllEarthquakesAsync()
        {
            try
            {
                var earthquakes = await _repository.GetAllAsync();

                var result = earthquakes
                    .OrderByDescending(eq => eq.EventTime)
                    .Select(MapToDto)
                    .ToList();

                _logger.LogInformation("Se obtuvieron {Count} terremotos desde SQL", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado consultando terremotos en SQL");
                throw;
            }
        }

        // Acción principal para filtrar terremotos por país usando los registros de SQL
        public async Task<List<EarthquakeDto>> GetEarthquakesByCountryAsync(string countryName)
        {
            if (string.IsNullOrWhiteSpace(countryName))
            {
                throw new BadRequestException("El nombre del país no puede estar vacío.");
            }

            var cleanCountryName = countryName.Trim();
            var earthquakes = await GetAllEarthquakesAsync();

            var filteredEarthquakes = earthquakes
                .Where(eq => !string.IsNullOrWhiteSpace(eq.Place) &&
                             eq.Place.Contains(cleanCountryName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Se filtraron {Filtered} terremotos para el país: {Country}",
                filteredEarthquakes.Count, cleanCountryName);

            return filteredEarthquakes;
        }

        private static EarthquakeDto MapToDto(Earthquake earthquake)
        {
            return new EarthquakeDto
            {
                Id = earthquake.Id,
                Magnitude = earthquake.Magnitude,
                Place = earthquake.Place ?? string.Empty,
                Time = earthquake.EventTime ?? DateTime.MinValue,
                Latitude = earthquake.Latitude ?? 0,
                Longitude = earthquake.Longitude ?? 0,
                DepthKm = earthquake.Depth,
                Url = earthquake.Url
            };
        }
    }
}
