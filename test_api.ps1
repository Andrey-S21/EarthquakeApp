# Script de prueba para verificar que la búsqueda de terremotos + país funciona

$apiUrl = "https://localhost:7069/api/earthquakes/search?country=italy"
$params = @{
	Uri = $apiUrl
	SkipCertificateCheck = $true
	Headers = @{ "Accept" = "application/json" }
	TimeoutSec = 10
}

try {
	Write-Host "Probando endpoint: $apiUrl" -ForegroundColor Cyan
	$response = Invoke-WebRequest @params

	Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green

	$data = $response.Content | ConvertFrom-Json

	if ($data.country) {
		Write-Host "✓ País encontrado: $($data.country.name)" -ForegroundColor Green
		Write-Host "  Capital: $($data.country.capital)" -ForegroundColor Green
		Write-Host "  Región: $($data.country.region)" -ForegroundColor Green
	} else {
		Write-Host "✗ Country es null" -ForegroundColor Red
	}

	if ($data.earthquakes) {
		Write-Host "✓ Terremotos encontrados: $($data.earthquakes.Count)" -ForegroundColor Green
	} else {
		Write-Host "✗ No hay terremotos" -ForegroundColor Yellow
	}
}
catch {
	Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
