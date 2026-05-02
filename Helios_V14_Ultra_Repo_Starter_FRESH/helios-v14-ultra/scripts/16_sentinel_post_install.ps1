$base = "C:\Helios\inventories"
Get-CimInstance Win32_StartupCommand | Export-Csv (Join-Path $base "startup.csv") -NoTypeInformation
Get-AppxPackage | Select Name, PackageFullName | Export-Csv (Join-Path $base "appx.csv") -NoTypeInformation
Write-Host "Sentinel baseline snapshot captured."
