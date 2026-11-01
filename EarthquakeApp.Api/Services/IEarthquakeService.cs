using EarthquakeApp.Dtos;

namespace EarthquakeApp.Api.Services
{
    // Esta interfaz define las promesas que se hacen del servicio de terremotos
    // Define qué métodos están disponibles
    public interface IEarthquakeService
    {
        // Se obtienen todos los terremotos almacenados en SQL
        Task<List<EarthquakeDto>> GetAllEarthquakesAsync();

        // Se obtienen datos de terremotos desde la API USGS
        // Se recibe el nombre del país y se devuelve una lista de terremotos
        Task<List<EarthquakeDto>> GetEarthquakesByCountryAsync(string countryName);
    }
}
