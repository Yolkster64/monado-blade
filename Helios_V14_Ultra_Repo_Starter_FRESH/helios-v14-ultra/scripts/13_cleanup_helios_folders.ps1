$paths = @(
    "C:\Helios\logs\*",
    "C:\Helios\inventories\*"
)
foreach ($p in $paths) {
    Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "Helios-managed folders cleaned."
