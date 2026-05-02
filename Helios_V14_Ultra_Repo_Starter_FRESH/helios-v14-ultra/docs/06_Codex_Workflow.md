# Codex Workflow

Use Codex/Copilot CLI to refine files one-by-one.

Example prompt:
Create `scripts/12_create_accounts.ps1` that preserves ADMIN, creates WORK/GAMING/DEV/MUSIC as standard users, logs actions, and is idempotent.

Recommended cycle:
1. Generate one file
2. Review
3. Test
4. Commit
5. Move to next file
