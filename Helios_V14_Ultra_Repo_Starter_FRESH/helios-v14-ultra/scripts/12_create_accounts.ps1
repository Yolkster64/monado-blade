$ErrorActionPreference = "Continue"
$users = @("WORK","GAMING","DEV","MUSIC")

foreach ($u in $users) {
    if (-not (Get-LocalUser -Name $u -ErrorAction SilentlyContinue)) {
        $pw = ConvertTo-SecureString "TempPass123!" -AsPlainText -Force
        New-LocalUser -Name $u -Password $pw -FullName $u
        Add-LocalGroupMember -Group "Users" -Member $u
        Write-Host "Created $u"
    } else {
        Write-Host "$u already exists"
    }
}
Write-Host "ADMIN is preserved and untouched by this script."
