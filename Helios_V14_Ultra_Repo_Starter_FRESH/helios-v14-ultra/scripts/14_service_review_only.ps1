$out = "C:\Helios\inventories\services_review.csv"
Get-Service | Select-Object Name, DisplayName, Status, StartType |
    Export-Csv $out -NoTypeInformation
Write-Host "Exported service review to $out"
