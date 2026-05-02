$ErrorActionPreference = "Continue"
$manifest = Join-Path $PSScriptRoot "..\manifests\software_manifest.json"
$data = Get-Content $manifest -Raw | ConvertFrom-Json

foreach ($app in $data.core) {
    winget install -e --id $app.id --silent --accept-package-agreements --accept-source-agreements
}
