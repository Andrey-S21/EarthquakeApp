using EarthquakeApp.Api.Configuration;
using EarthquakeApp.Api.Caching;
using EarthquakeApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using EarthquakeApp.Data.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EarthquakeDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EarthquakeDB")));

builder.Services.AddScoped<IEarthquakeRepository, EarthquakeRepository>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

app.UseMiddleware<EarthquakeApp.Api.Middleware.ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
