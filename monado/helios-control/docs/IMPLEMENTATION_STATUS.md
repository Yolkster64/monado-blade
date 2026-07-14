# Helios integration implementation status

## Implemented in this change

- Buildable .NET 8 webhook/API project and tests.
- GitHub, Linear, and Slack HMAC verification in live mode.
- Slack replay-window enforcement, request bounds, JSON validation, and process-local duplicate rejection.
- Local-only, read-only MCP tools for Copilot Chat.
- Azure Bicep resources for identity, Key Vault, Service Bus subscription, ADLS evidence, Cosmos candidates, AI Search, ACR, Container Apps, Log Analytics, Application Insights, and optional APIM.
- OIDC Azure what-if workflow and separate Copilot package validation workflow.
- OpenAI Responses provider with explicit model selection and environment/Key Vault credential references.
- Microsoft identity, toolchain, Agent 365, Teams/Copilot package, and approval contracts.

## Intentionally not enabled

- Teams, SharePoint, Foundry, and Copilot inbound webhooks fail closed until Entra JWT and validation-challenge middleware is configured.
- Remote MCP is not exposed. The current MCP endpoint accepts loopback development traffic only and is disabled in live mode.
- `hermes.run_sandbox`, task generation, evaluation writes, and promotion tools are not exposed.
- Tenant-wide Graph consent, Copilot publication, Conditional Access changes, production RBAC, and Azure deployment require explicit administrator approval.

## Attached Python source reality

The uploaded Hermes/XCore Python files are prototypes. The current training loop
depends on a missing `hermes_xcore` package and generates synthetic quality/latency
scores rather than training a model. VM orchestration and ML registry files are
metadata scaffolds. They must not write governed learning state until normalized
into a package with real sandbox execution, evidence, evaluation, lineage, and rollback.

## Next vertical slice

1. Green CI and Bicep validation.
2. Shared versioned event schemas with `M0nado/helios-platform`.
3. Service Bus persistence and Cosmos idempotency.
4. Real isolated worker evidence, marked synthetic until validated.
5. Entra-authenticated remote MCP and Foundry Hosted Agent.
6. Evaluated, versioned publication to Microsoft 365 Copilot and Teams.
