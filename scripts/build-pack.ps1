# Build and pack PandoDeploy for local testing
param(
    [string]$Version = "1.0.0-local"
)

Write-Host "Building PandoDeploy version $Version" -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean --configuration Release

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build
Write-Host "Building..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore /p:Version=$Version

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity minimal

# Pack CLI tool
Write-Host "Packing CLI tool..." -ForegroundColor Yellow
$packOutput = "./nupkgs"
if (Test-Path $packOutput) {
    Remove-Item $packOutput -Recurse -Force
}
New-Item -ItemType Directory -Path $packOutput | Out-Null

dotnet pack src/PandoDeploy.CLI/PandoDeploy.CLI.csproj --configuration Release --no-build --output $packOutput /p:Version=$Version

Write-Host "`nPackage created successfully!" -ForegroundColor Green
Write-Host "To install locally, run:" -ForegroundColor Cyan
Write-Host "  dotnet tool uninstall -g PandoDeploy" -ForegroundColor White
Write-Host "  dotnet tool install -g PandoDeploy --add-source $packOutput --version $Version" -ForegroundColor White

