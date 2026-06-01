# WorldCupPredictor - start both apps
# Usage: .\run.ps1

$root   = $PSScriptRoot
$apiDir = Join-Path $root "src\WorldCupPredictor.API"
$ngDir  = Join-Path $root "world-cup-predictor"

Write-Host "WC26 Predictor - Starting apps..." -ForegroundColor Cyan

# Kill anything on ports 5000 / 4200
foreach ($port in @(5000, 4200)) {
    $pids = netstat -ano 2>$null |
            Select-String (":$port\s") |
            ForEach-Object { ($_ -split '\s+')[-1] } |
            Where-Object { $_ -match '^\d+$' } |
            Select-Object -Unique
    foreach ($p in $pids) { Stop-Process -Id $p -Force -ErrorAction SilentlyContinue }
}

Start-Sleep -Milliseconds 500

# Start API
Write-Host "[1/2] Starting .NET API  -> http://localhost:5000" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$apiDir'; dotnet run --urls http://localhost:5000"

# Start Angular
Write-Host "[2/2] Starting Angular   -> http://localhost:4200" -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ngDir'; npx ng serve --open"

Write-Host "Done. Browser will open at http://localhost:4200 once Angular compiles." -ForegroundColor Green
