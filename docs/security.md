# Security Guide

## Security Foundations

- C++/C# components own low-level hardening and partition isolation.
- PowerShell is used for controlled provisioning, not persistent privileged automation.
- Vault and Quarantine are first-class security domains with explicit audit trails.
- Admin-level unlock actions require local, physical proof-of-presence.

## Admin and Unlock Flow

- Administrative partitions remain local-only and are not remotely unlockable.
- Unlocking Vault or Quarantine requires local approval plus hardware/MFA gates.
- Firmware, driver, and baseline integrity checks run on unlock and can trigger rollback paths.

## Runtime Security Automation

- Agent telemetry tracks process/resource drift and suspicious behavior.
- AI automation cannot self-escalate privileges; privileged actions require explicit policy.
- Data-leak prevention and file-watch controls should remain enabled in hardened profiles.

## Security Baseline Checklist

1. Secrets are stored outside source control.
2. CI/CD identities use least privilege.
3. API endpoints enforce authorization and input validation.
4. TLS is enabled for all external traffic.
5. Audit logs are retained and queryable.
6. Incident response contacts and rotation runbooks are current.
