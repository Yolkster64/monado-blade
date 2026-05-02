# First Boot Checklist

1. Reset Windows with Remove everything + Cloud download
2. Sign into the local ADMIN account
3. Update Windows fully
4. Enable BitLocker and Windows Hello
5. Run `scripts/10_bootstrap_core.ps1`
6. Reboot
7. Run `scripts/11_install_core_apps.ps1`
8. Run `scripts/12_create_accounts.ps1`
9. Review `inventories/` outputs
10. Run Sentinel tasks after baseline review
