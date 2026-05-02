$base = "C:\Helios\inventories"
Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall" |
    Get-ItemProperty |
    Select DisplayName, DisplayVersion, Publisher |
    Export-Csv (Join-Path $base "installed_programs.csv") -NoTypeInformation
