using EarthquakeApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace EarthquakeApp.Data.Repositories
{
    public class EarthquakeRepository : IEarthquakeRepository //Obliga a implementar los métodos de la interfaz.
    {
        private readonly EarthquakeDbContext _context;  //Permite acceder a nuestra base de datos mediante Entity Framework.

        public EarthquakeRepository(EarthquakeDbContext context)
        {
            _context = context;
        }

        public async Task<List<Earthquake>> GetAllAsync()
        {
            return await _context.Earthquakes.ToListAsync(); //Obtiene los terremotos almacenados.
        }

        public async Task<Earthquake?> GetByIdAsync(string id)
        {
            return await _context.Earthquakes.FindAsync(id); //Busca un terremoto por su identificador.
        }

        public async Task AddAsync(Earthquake earthquake) //Agrega un terremoto al contexto.
        {
            await _context.Earthquakes.AddAsync(earthquake);
            await _context.SaveChangesAsync();  //Guarda los cambios en SQL Server.
        }
    }
}