# HELIOS integration pack import status

## Source audit

- Archive: `helios_integration_apps_drivers_pack.zip`
- Extracted file count: **37**
- SHA-256: `575e0760852f6bf690a297611c1ccef502100e1390cd4b18d53787ef65a5cffe`
- Local manifest tests: **4 passed**

## Included automation on this branch

- JSON parsing and bundle-to-catalog reference tests.
- Security confirmation-gate assertions.
- Dry-run installer assertion.
- .NET builds for hardware detection and software catalog projects.
- PowerShell parser validation.
- Bicep static build.
- Manually dispatched Azure OIDC resource-group `what-if` only.

## Import gate

The 37 source files have been audited locally, but the current GitHub connector write surface accepts UTF-8 file contents and cannot upload the source ZIP as binary. The draft PR must remain unmerged until the unpacked pack is added through a binary-capable Git client or the files are imported individually. No live Azure deployment is enabled.

## Required GitHub environment

Environment: `azure-what-if`

Secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Variable:

- `AZURE_RESOURCE_GROUP`

The federated identity should be scoped to this repository and the `azure-what-if` environment. Grant only the minimum role needed to evaluate the target resource group.
