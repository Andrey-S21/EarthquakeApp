\## Memory Caching (investigación)



\*\*¿Qué es?\*\* `IMemoryCache` guarda objetos en la memoria del proceso del servidor. Es muy rápido, pero los datos se pierden al reiniciar la aplicación y no se comparten entre varias instancias del API.



\*\*¿Por qué se usa aquí?\*\* Evita repetir llamadas a USGS y REST Countries para la misma búsqueda, reduce la latencia de respuesta y protege los límites de uso de las APIs externas.



\*\*Implementación\*\*

\- Servicio centralizado `ICacheService` (`EarthquakeApp.Api/Caching`) con el método `GetOrCreateAsync`: si el dato está en caché lo devuelve; si no, ejecuta la llamada real y guarda el resultado.

\- Expiración absoluta, configurable en `appsettings.json` (`Cache:CountryMinutes` y `Cache:EarthquakeMinutes`).

\- Claves normalizadas por país (`country:costa rica`, `earthquakes:costa rica`), para que "Costa Rica" y "costa rica" usen la misma entrada.

\- Si la llamada externa falla, no se guarda nada en caché, así un error no queda almacenado.



\## Configuración y secrets



La URL base y la API key de REST Countries se leen desde `appsettings.json` del proyecto API mediante `IOptions<T>` (`RestCountriesOptions`, `UsgsOptions`, `CacheOptions`).



\## Manejo de excepciones en el API



Un middleware (`ExceptionHandlingMiddleware`) captura las excepciones y devuelve un JSON uniforme `{ "status": ..., "message": ... }` con los códigos 400, 404, 503 o 500, sin exponer detalles internos.

