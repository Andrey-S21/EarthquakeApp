using EarthquakeApp.Api.Configuration;
using EarthquakeApp.Api.Caching;
using EarthquakeApp.Api.Services;
using EarthquakeApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using EarthquakeApp.Data.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EarthquakeDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EarthquakeDB")));

builder.Services.AddScoped<IEarthquakeRepository, EarthquakeRepository>();

// Registrar HttpClient para hacer peticiones a APIs externas
builder.Services.AddHttpClient();

// Registrar el servicio de terremotos
builder.Services.AddScoped<IEarthquakeService, EarthquakeService>();

// Registrar el servicio de países
builder.Services.AddScoped<ICountryService, CountryService>();

// Se agregan los servicios necesarios para la API
builder.Services.AddControllers();

// Se configura OpenAPI para la documentación interactiva de la API
builder.Services.AddOpenApi();

builder.Services.Configure<RestCountriesOptions>(
        builder.Configuration.GetSection(RestCountriesOptions.SectionName));
builder.Services.Configure<UsgsOptions>(
        builder.Configuration.GetSection(UsgsOptions.SectionName));
builder.Services.Configure<CacheOptions>(
        builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

var app = builder.Build();

// Se usa middleware personalizado para manejar excepciones
app.UseMiddleware<EarthquakeApp.Api.Middleware.ExceptionHandlingMiddleware>();

// Se configura el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    // Se habilita OpenAPI para explorar la API en tiempo de desarrollo
    app.MapOpenApi();

    // Se proporciona una página UI simple para explorar la API
    app.MapGet("/swagger", context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Earthquake API - Documentación</title>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <link href='https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700' rel='stylesheet'>
    <style>
        body {
            margin: 0;
            padding: 0;
        }
    </style>
</head>
<body>
    <redoc spec-url='./openapi/v1.json'></redoc>
    <script src='https://cdn.jsdelivr.net/npm/redoc@next/bundles/redoc.standalone.js'></script>
</body>
</html>
");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.Run();
