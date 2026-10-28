using EarthquakeApp.Api.Exceptions;
using EarthquakeApp.Api.Services;
using EarthquakeApp.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace EarthquakeApp.Api.Controllers
{
    // Este controlador se encarga de manejar las peticiones relacionadas con terremotos
    // Se combinan los servicios de terremotos y países para entregar una respuesta unificada
    [ApiController]
    [Route("api/[controller]")]
    public class EarthquakesController : ControllerBase
    {
        // Estas son las herramientas necesarias para funcionar
        private readonly IEarthquakeService _earthquakeService;      // Se utiliza para obtener datos de terremotos
        private readonly ICountryService _countryService;            // Se utiliza para obtener datos del país
        private readonly ILogger<EarthquakesController> _logger;     // Se utiliza para escribir registros/logs

        // Constructor: aquí se reciben todas las herramientas
        public EarthquakesController(
            IEarthquakeService earthquakeService,
            ICountryService countryService,
            ILogger<EarthquakesController> logger)
        {
            _earthquakeService = earthquakeService;
            _countryService = countryService;
            _logger = logger;
        }

        // Endpoint para buscar terremotos por país
        // GET: /api/earthquakes/search?country=Japan
        [HttpGet("search")]
        public async Task<ActionResult<SearchResponseDto>> SearchByCountry([FromQuery] string country)
        {
            try
            {
                // Paso 1: Se valida que el país no esté vacío
                if (string.IsNullOrWhiteSpace(country))
                {
                    _logger.LogWarning("Se recibió una búsqueda sin especificar país");
                    throw new BadRequestException("El nombre del país es requerido.");
                }

                // Paso 2: Se limpia el país (se eliminan espacios extras)
                var cleanCountry = country.Trim();

                // Paso 3: Se valida la longitud (máximo 100 caracteres según enunciado)
                if (cleanCountry.Length > 100)
                {
                    _logger.LogWarning("Nombre de país demasiado largo: {Country}", cleanCountry);
                    throw new BadRequestException("El nombre del país no puede exceder 100 caracteres.");
                }

                // Paso 4: Se valida que solo contenga letras, espacios y guiones
                if (!Regex.IsMatch(cleanCountry, @"^[a-zA-Z\s\-]+$"))
                {
                    _logger.LogWarning("Nombre de país con caracteres inválidos: {Country}", cleanCountry);
                    throw new BadRequestException("El nombre del país solo puede contener letras, espacios y guiones.");
                }

                // Paso 5: Se obtienen datos de terremotos
                var earthquakes = await _earthquakeService.GetEarthquakesByCountryAsync(cleanCountry);

                // Paso 6: Se obtiene información del país (tolerante a fallos)
                CountryDto? country_info = null;
                try
                {
                    country_info = await _countryService.GetCountryByNameAsync(cleanCountry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener información del país");
                }

                // Paso 7: Se construye la respuesta unificada
                var response = new SearchResponseDto
                {
                    Country = country_info,
                    TotalEarthquakes = earthquakes.Count,
                    Earthquakes = earthquakes
                };

                return Ok(response);
            }
            catch (BadRequestException ex)
            {
                // Se capturan excepciones de validación
                _logger.LogWarning("Error de validación: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = ex.Message
                });
            }
            catch (NotFoundException ex)
            {
                // Se capturan excepciones de no encontrado
                _logger.LogWarning("Recurso no encontrado: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto
                {
                    Status = StatusCodes.Status404NotFound,
                    Message = ex.Message
                });
            }
            catch (ExternalServiceException ex)
            {
                // Se capturan excepciones de servicios externos
                _logger.LogError(ex, "Error en servicio externo");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponseDto
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Se captura cualquier otro error inesperado
                _logger.LogError(ex, "Error inesperado en búsqueda de terremotos");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = "Ocurrió un error inesperado. Por favor, intenta más tarde."
                });
            }
        }
    }
}
