# AI Integration Guide

## Philosophy & Overview
- Hermes “AI Hub/Builder” maximizes AI flexibility—train and serve locally with Python/C++/C#, escalate to Azure/OpenAI/Foundry as needed.
- Designs influenced by Monado Blade (hybrid optimization), Herm-es Fleet (local orchestrator), and Helios Automation (security/monitor/AI integration).
- All major AIs, CLIs and shells, and local/cloud communication APIs are supported, with cross-process security.

## Local and Cloud Blending
- Run/train/test LLMs and ML models in `/ai` using Python, C#, or C++ for best perf
- DevDrive (VHDX), ChromaDB, Synapse AI pipelines, and Foundry integrations all documented here
- Any model can be auto-versioned, swapped, updated, or sandboxed for security

## Integration Tools
- Native CLIs and scripts for Azure, Power Platform, Copilot Studio, OpenAI API, Designer, Foundry
- /integrations guides with code samples (see `/integrations/README.md`)

## AI Troubleshooting
- AI module and ops logging is always-on, with `/tests` and `/scripts` for validation
- AI dashboards help visualize and audit all activity, both local and in cloud
