Set-Location $PSScriptRoot
docker compose up -d --build
docker compose ps

$api = "http://localhost:8787"
$gateway = "http://localhost:8788"
for ($i = 0; $i -lt 20; $i++) {
    try {
        Invoke-RestMethod -Uri "$api/health" -Method Get | Out-Null
        break
    } catch {
        Start-Sleep -Seconds 2
    }
}

for ($i = 0; $i -lt 20; $i++) {
    try {
        Invoke-RestMethod -Uri "$gateway/health" -Method Get | Out-Null
        break
    } catch {
        Start-Sleep -Seconds 2
    }

    try {
        Invoke-RestMethod -Uri "$gateway/learning-pulse" -Method Post -ContentType "application/json" -Body '{"specialty":"fleet","steps":60,"candidates":40}' | Out-Null
    } catch {
        Write-Host "Warm-start pulse skipped (gateway not ready yet)."
    }
}

Write-Host "Hermes API: http://localhost:8787"
Write-Host "Hermes Gateway (C# front): http://localhost:8788"
Write-Host "Hermes GUI: http://localhost:8501"
