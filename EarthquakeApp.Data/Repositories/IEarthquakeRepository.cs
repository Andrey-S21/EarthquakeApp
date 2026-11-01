using EarthquakeApp.Data.Models;

namespace EarthquakeApp.Data.Repositories
{
    public interface IEarthquakeRepository
    {
        Task<List<Earthquake>> GetAllAsync(); // Obtiene todos los terremotos guardados.

        Task<Earthquake?> GetByIdAsync(string id); //busca  un terremoto mediante su identificador. 

        Task AddAsync(Earthquake earthquake);//guarda un nuevo evento de terremoto 

        // Se guardan muchos terremotos de una sola vez (mucho más rápido que uno por uno)
        Task AddRangeAsync(IEnumerable<Earthquake> earthquakes);

        // Se obtiene la fecha del terremoto más reciente guardado (null si la tabla está vacía)
        Task<DateTime?> GetLatestEventTimeAsync();

        // Se revisa cuáles de los IDs indicados ya existen en la base de datos
        Task<HashSet<string>> GetExistingIdsAsync(IEnumerable<string> ids);
    }
}