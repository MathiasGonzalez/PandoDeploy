# Create an API key in the PandoDeploy database
param(
    [string]$DbPath = "./data/pandodeploy.db",
    [string]$KeyName = "default"
)

if (-not (Test-Path $DbPath)) {
    Write-Host "Database not found at $DbPath" -ForegroundColor Red
    Write-Host "Please start the server first or specify the correct path"
    exit 1
}

$ApiKey = [guid]::NewGuid().ToString("N")

# Check if sqlite3 is available, otherwise provide manual instructions
$sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue

if ($sqlite) {
    sqlite3 $DbPath "INSERT INTO ApiKeys (Key, Name, IsActive, CreatedAt) VALUES ('$ApiKey', '$KeyName', 1, datetime('now'));"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "API Key created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Name: $KeyName" -ForegroundColor Cyan
        Write-Host "Key: $ApiKey" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Export it as environment variable:" -ForegroundColor White
        Write-Host "  `$env:PANDODEPLOY_API_KEY='$ApiKey'" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Or use it directly in commands:" -ForegroundColor White
        Write-Host "  pandodeploy deploy --image myapp:latest --server http://localhost:5000 --api-key $ApiKey" -ForegroundColor Gray
    } else {
        Write-Host "Failed to create API key" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "SQLite3 not found. Please run this SQL manually:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "INSERT INTO ApiKeys (Key, Name, IsActive, CreatedAt) VALUES ('$ApiKey', '$KeyName', 1, datetime('now'));" -ForegroundColor White
    Write-Host ""
    Write-Host "API Key: $ApiKey" -ForegroundColor Yellow
}

