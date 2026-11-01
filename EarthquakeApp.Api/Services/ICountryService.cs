using EarthquakeApp.Dtos;

namespace EarthquakeApp.Api.Services
{
    // Esta interfaz define las promesas que se hacen del servicio de países
    // Define qué métodos están disponibles
    public interface ICountryService
    {
        // Se obtiene información de un país por su nombre
        // Se recibe el nombre del país y se devuelve los datos del país (o null si no existe)
        Task<CountryDto?> GetCountryByNameAsync(string countryName);
    }
}
