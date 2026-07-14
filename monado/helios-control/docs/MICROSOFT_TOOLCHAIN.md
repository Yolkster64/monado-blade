# Microsoft developer and Copilot toolchain

Helios uses one reproducible developer lane across Codespaces, local Dev Drive,
GitHub Actions, and Azure DevOps. `config/microsoft-toolchain.json` is the inventory;
the devcontainer supplies the baseline tools; `azd` and Bicep deploy reviewed
infrastructure; tenant applications are registered separately through Entra and
solution pipelines.

## Merge boundaries

- Azure CLI, `azd`, and Bicep manage Azure resources through OIDC or interactive developer identity.
- Power Platform CLI moves versioned Copilot Studio solutions between environments.
- Microsoft 365 Agents Toolkit (`atk`) packages published agents for Copilot and Teams; new work does not use deprecated TeamsFx.
- GitHub CLI and Copilot work through branches and pull requests.
- Azure DevOps remote MCP is configured read-only for supported local VS Code/Visual Studio clients. Broader Foundry or Copilot Studio support is not assumed while the remote server remains preview-limited.
- Azure Functions exposes the Helios MCP endpoint at `/runtime/webhooks/mcp`.
- Foundry Hosted Agents run Hermes orchestration code with dedicated Entra identities.

## Authentication

Developer login is interactive. CI uses federated workload identity. Production
agents use managed identity. Key Vault is used only for services that cannot use
federation. PATs, client secrets, raw tokens, and recovery material never enter
source control or agent memory.

## Provisioning order

1. Validate GitHub branch protection and CI.
2. Create Entra groups, workload identities, and least-privilege roles.
3. Run `azd provision --preview`, Bicep lint, and Azure what-if.
4. Deploy shared data, messaging, identity, and observability resources.
5. Register the Foundry toolbox and Helios MCP endpoint.
6. Deploy Hermes/XCore agents into a development Foundry project.
7. Evaluate and trace before publishing to Microsoft 365 Copilot or Teams.
8. Export Copilot Studio solutions and promote through governed environments.

Published Foundry agents receive dedicated identities. Development RBAC does not
silently transfer to published versions; production permissions are reassigned
explicitly after publication and before traffic promotion.
