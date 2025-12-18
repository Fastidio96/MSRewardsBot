# Require Admin / UAC elevation
If (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "Restarting script with Administrator privileges..."
    Start-Process powershell -Verb RunAs -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"" + $MyInvocation.MyCommand.Path + "`"")
    Exit
}

# Current script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Project root directory (parent of Scripts)
$ProjectPath = Split-Path -Parent $ScriptDir

Set-Location $ProjectPath

# Multi-RID publish
$RIDs = "win-x64","linux-x64"

foreach ($rid in $RIDs) {
    Write-Host "`nPublishing for $rid..."
    dotnet publish -c Release -r $rid -o (Join-Path $ProjectPath "publish\$rid") --self-contained false
}

# Delete publish folder if it exists
$PublishFolder = Join-Path $ProjectPath "publish"
if (Test-Path $PublishFolder) {
    Remove-Item $PublishFolder -Recurse -Force
    Write-Host "Deleted folder: $PublishFolder"
}

Write-Host "`nPublish completed successfully. Press Enter to exit..."
Read-Host
