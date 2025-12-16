$serviceName = "MSRewardsBot.Server"
$displayName = "MSRewardsBot Server Service"

# Resolve current folder exe
$exeName = (Get-ChildItem -Filter *.exe | Select-Object -First 1).Name
$exePath = Join-Path (Get-Location) $exeName

if (-not (Test-Path $exePath)) {
    Write-Error "No executable found in current directory."
    exit 1
}

# Stop and remove service if it already exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service already exists. Recreating..."
    if ($existingService.Status -ne "Stopped") {
        Stop-Service -Name $serviceName -Force
    }
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

# Create service
sc.exe create $serviceName `
    binPath= "`"$exePath`"" `
    start= auto `
    DisplayName= "`"$displayName`""

# Configure recovery: restart after 30 minutes (1800 seconds) on failure
sc.exe failure $serviceName reset= 86400 actions= restart/1800000/restart/1800000/restart/1800000

# Start service
Start-Service -Name $serviceName

Write-Host "Service installed and started successfully."
Read-Host
