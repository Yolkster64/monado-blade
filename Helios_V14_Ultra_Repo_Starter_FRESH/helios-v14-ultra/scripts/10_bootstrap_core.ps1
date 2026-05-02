$ErrorActionPreference = "Stop"
$base = "C:\Helios"
New-Item -ItemType Directory -Force -Path $base,$base\logs,$base\inventories,$base\scripts,$base\templates | Out-Null

# Security baseline
Set-MpPreference -DisableRealtimeMonitoring $false
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart | Out-Null

Write-Host "Helios bootstrap core complete. Reboot recommended."
