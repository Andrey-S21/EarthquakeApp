using EarthquakeApp.Data.Models;

namespace EarthquakeApp.Data.Repositories
{
    public interface IEarthquakeRepository
    {
        Task<List<Earthquake>> GetAllAsync(); // Obtiene todos los terremotos guardados.

        Task<Earthquake?> GetByIdAsync(string id); //busca  un terremoto mediante su identificador. 

        Task AddAsync(Earthquake earthquake);//guarda un nuevo evento de terremoto 
    }
}