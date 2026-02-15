# Publishes and serves the application on a fixed port matching launchSettings (HTTP: 5073)

Write-Host "Building and Publishing ShYCalculator.Wasm..."
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) { 
    Write-Error "Publish failed."
    exit $LASTEXITCODE 
}

$publishDir = "bin\Release\net10.0\publish\wwwroot"
if (!(Test-Path $publishDir)) {
    Write-Error "Publish directory not found: $publishDir"
    exit 1
}

Write-Host "Serving from $publishDir on port 5073..."
Push-Location $publishDir
try {
    # -o opens the browser, -p sets the port
    dotnet serve -p 5073 -o
}
finally {
    Pop-Location
    Write-Host "Server stopped."
}
