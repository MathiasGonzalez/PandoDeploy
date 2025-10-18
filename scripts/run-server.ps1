# Run PandoDeploy server

Set-Location $PSScriptRoot\..

Write-Host "Starting PandoDeploy Server..." -ForegroundColor Green
Write-Host ""

# Create data directories if they don't exist
New-Item -ItemType Directory -Path "./data/images" -Force | Out-Null

# Run the server
dotnet run --project src/PandoDeploy.Server/PandoDeploy.Server.csproj

