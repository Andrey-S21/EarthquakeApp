using EarthquakeApp.Web.Models;
using EarthquakeApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EarthquakeApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEarthquakeApiService _earthquakeApiService;

        public HomeController(IEarthquakeApiService earthquakeApiService)
        {
            _earthquakeApiService = earthquakeApiService;
        }

        // Acción principal de la página: lista terremotos y procesa búsqueda por país
        public async Task<IActionResult> Index(string? country)
        {
            var model = new HomeIndexViewModel
            {
                SearchTerm = country?.Trim() ?? string.Empty
            };

            try
            {
                if (string.IsNullOrWhiteSpace(model.SearchTerm))
                {
                    model.Earthquakes = await _earthquakeApiService.GetAllEarthquakesAsync();
                    model.CountryInfo = null;
                }
                else
                {
                    var response = await _earthquakeApiService.SearchByCountryAsync(model.SearchTerm);
                    model.Earthquakes = response.Earthquakes;
                    model.CountryInfo = response.Country;

                    if (model.CountryInfo == null)
                    {
                        model.UserMessage = "No se encontró información del país solicitado.";
                    }
                    else if (!model.Earthquakes.Any())
                    {
                        model.UserMessage = "No se encontraron terremotos para el país buscado.";
                    }
                }
            }
            catch (Exception ex)
            {
                model.Earthquakes = new List<EarthquakeApp.Dtos.EarthquakeDto>();
                model.CountryInfo = null;
                model.UserMessage = $"Ocurrió un problema al consultar la información: {ex.Message}";
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
