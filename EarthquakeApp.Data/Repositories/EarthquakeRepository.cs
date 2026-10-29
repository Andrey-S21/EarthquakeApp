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

        // Se agregan todos los terremotos recibidos y se guardan en un solo viaje a SQL Server
        public async Task AddRangeAsync(IEnumerable<Earthquake> earthquakes)
        {
            await _context.Earthquakes.AddRangeAsync(earthquakes);
            await _context.SaveChangesAsync();

            // Se limpia la memoria de EF para que no se vuelva lenta con miles de registros
            _context.ChangeTracker.Clear();
        }

        // Se busca la fecha más reciente entre todos los terremotos guardados
        public async Task<DateTime?> GetLatestEventTimeAsync()
        {
            return await _context.Earthquakes.MaxAsync(e => e.EventTime);
        }

        // Se consultan en la base de datos los IDs que ya están guardados, para no repetirlos
        public async Task<HashSet<string>> GetExistingIdsAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            var existing = new HashSet<string>();

            // Se consulta por grupos de 1000 porque SQL Server tiene un límite de parámetros por consulta
            foreach (var chunk in idList.Chunk(1000))
            {
                var found = await _context.Earthquakes
                    .Where(e => chunk.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();

                existing.UnionWith(found);
            }

            return existing;
        }
    }
}