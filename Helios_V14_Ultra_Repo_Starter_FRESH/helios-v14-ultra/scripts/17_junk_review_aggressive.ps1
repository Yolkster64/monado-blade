Write-Host "Review Appx and classic uninstall candidates manually before removal."
Get-AppxPackage | Select Name, PackageFullName | Out-GridView -Title "Appx Review"
