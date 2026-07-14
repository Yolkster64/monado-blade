# Helios Connect

Helios Connect is the local-first integration control plane for GitHub, Linear,
Slack, Microsoft Teams, SharePoint, Microsoft Copilot/Foundry, Azure, and MCP.

It uses one normalized event envelope, an allowlisted router, idempotency keys,
and per-connector workers. Secrets never live in source control: local secrets
come from environment variables or .NET user-secrets; Azure secrets come from
Key Vault through managed identity.

## Quick start

1. Copy `.env.example` to `.env` and fill only the connectors you need.
2. Install .NET 8 SDK.
3. Run `dotnet restore && dotnet test`.
4. Run `dotnet run --project src/Helios.Connect.Api`.
5. Point webhook providers to `https://<host>/webhooks/{provider}`.

The default profile is `dry-run`: incoming events are validated and logged but
no external writes occur. Set `HELIOS_EXECUTION_MODE=live` only after Key Vault,
signing secrets, and destination allowlists are configured.

See `docs/ARCHITECTURE.md`, `docs/CONNECTION_RUNBOOK.md`, and
`config/integrations.json`.
