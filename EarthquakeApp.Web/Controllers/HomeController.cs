using EarthquakeApp.Dtos;
using EarthquakeApp.Web.Models;
using EarthquakeApp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EarthquakeApp.Web.Controllers
{
    public class HomeController : Controller
    {
        private const int MaxSearchLength = 100;
        private static readonly Regex CountryNameRegex =
            new(@"^[\p{L}\p{M}][\p{L}\p{M}\s.'’\-]*$", RegexOptions.Compiled);

        private readonly IEarthquakeApiService _earthquakeApiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IEarthquakeApiService earthquakeApiService, ILogger<HomeController> logger)
        {
            _earthquakeApiService = earthquakeApiService;
            _logger = logger;
        }

        // Valida el texto del buscador (devuelve null si es válido)
        private static string? ValidateSearchTerm(string term)
        {
            if (string.IsNullOrEmpty(term)) return null;

            if (term.Length > MaxSearchLength)
                return $"El nombre del país no puede superar los {MaxSearchLength} caracteres.";

            if (!CountryNameRegex.IsMatch(term))
                return "Solo se permiten letras, espacios, puntos, apóstrofes y guiones en el nombre del país.";

            return null;
        }

        // Solo prepara la alerta (los resultados que ya existan se conservan)
        private static void Notify(HomeIndexViewModel model, string icon, string title, string message)
        {
            model.AlertIcon = icon;
            model.AlertTitle = title;
            model.UserMessage = message;
        }

        // Deja el modelo en un estado seguro (vuelve al landing) y prepara la alerta
        private static void Fail(HomeIndexViewModel model, string icon, string title, string message)
        {
            model.Earthquakes = new List<EarthquakeDto>();
            model.CountryInfo = null;
            model.TotalItems = 0;
            model.HasSearched = false;
            Notify(model, icon, title, message);
        }

        // Página única: muestra el buscador y, si hay búsqueda, los terremotos del país
        public async Task<IActionResult> Index(string? country, int page = 1)
        {
            const int pageSize = 25;
            page = page < 1 ? 1 : page;

            var model = new HomeIndexViewModel
            {
                SearchTerm = Regex.Replace(country?.Trim() ?? string.Empty, @"\s+", " "),
                CurrentPage = page,
                PageSize = pageSize
            };

            // Validación de entrada (en el servidor, por si se salta la del navegador)
            var validationError = ValidateSearchTerm(model.SearchTerm);
            if (validationError != null)
            {
                Notify(model, "warning", "Búsqueda no válida", validationError);
                return View(model);
            }

            // Sin búsqueda: solo se muestra el landing con el buscador
            if (string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                return View(model);
            }

            try
            {
                var response = await _earthquakeApiService.SearchByCountryAsync(model.SearchTerm);
                var allEarthquakes = response.Earthquakes ?? new List<EarthquakeDto>();

                model.HasSearched = true;
                model.CountryInfo = response.Country;

                if (model.CountryInfo == null)
                {
                    Notify(model, "info", "País no encontrado",
                        "No se encontró información del país solicitado. Revisa que el nombre esté bien escrito.");
                }
                else if (!allEarthquakes.Any())
                {
                    Notify(model, "info", "Sin terremotos",
                        "No se encontraron terremotos registrados para el país buscado.");
                }

                model.TotalItems = allEarthquakes.Count;

                if (model.TotalPages > 0 && model.CurrentPage > model.TotalPages)
                {
                    model.CurrentPage = model.TotalPages;
                }

                var skip = (model.CurrentPage - 1) * model.PageSize;
                model.Earthquakes = allEarthquakes
                    .Skip(skip)
                    .Take(model.PageSize)
                    .ToList();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con la API de sismos.");
                Fail(model, "error", "Servidor no disponible",
                    "No se pudo conectar con el servidor de sismos. Verifica que la API esté en ejecución e inténtalo de nuevo.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "La consulta a la API de sismos excedió el tiempo de espera.");
                Fail(model, "warning", "Tiempo de espera agotado",
                    "La consulta tardó demasiado en responder. Inténtalo nuevamente en unos momentos.");
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                _logger.LogError(ex, "La API de sismos devolvió una respuesta con formato inesperado.");
                Fail(model, "error", "Respuesta inesperada",
                    "El servidor respondió con un formato inesperado. Inténtalo más tarde.");
            }
            catch (InvalidOperationException ex)
            {
                // Errores que manda la API (400, 404, 503...) con su mensaje ya en español
                _logger.LogWarning(ex, "La API de sismos devolvió un error controlado.");
                Fail(model, "warning", "No se pudo completar la búsqueda", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al procesar la búsqueda de sismos.");
                Fail(model, "error", "Error inesperado",
                    "Ocurrió un error inesperado. Inténtalo nuevamente en unos momentos.");
            }

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
