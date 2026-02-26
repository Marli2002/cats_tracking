Write-Host "WARNING: This has not been tested on Windows yet."

$api = Start-Process dotnet -Argument "run --project .\CATSTracking.API\CATSTracking.API.csproj"
$ui = Start-Process dotnet -Argument "run --project .\CATSTracking.UI\CATSTracking.UI.csproj"

Write-Host "API: http://localhost:6000/swagger"
Write-Host "UI: http://localhost:5000"

Read-Host "Press any key to shutdown apps..."

$api | Stop-Process
$ui | Stop-Process
