Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True
Write-Host "Firewall enabled. Add app-specific rules after baseline review."
