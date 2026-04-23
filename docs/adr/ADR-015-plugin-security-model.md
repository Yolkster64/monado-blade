# ADR-015: Plugin Security and Sandbox Model

## Status
Accepted

## Context
Plugins extend functionality but introduce security risks. Untrusted plugins must be safely contained.

## Problem
How to support plugins while protecting core system from malicious or buggy plugins?

## Decision
Implement plugin security through multiple layers:
1. Code signing verification for all plugins
2. Plugin manifest with declared permissions
3. Runtime permission enforcement
4. Plugin API limits what plugins can access
5. Separate AppDomain/AssemblyLoadContext for plugins
6. Plugin execution monitoring
7. Plugin resource limits (memory, CPU)
8. Plugin sandboxing for untrusted sources

## Consequences

### Positive
- Malicious plugins limited in damage
- Clear API prevents accidental core corruption
- Permission model enables user control
- Resource limits prevent DoS
- Code signing prevents tampering

### Negative
- Plugin development more restricted
- Signing infrastructure required
- Performance overhead from security checks
- Legitimate use cases may be blocked

## Alternatives Considered

1. **No Plugin Security**: Simple but dangerous
2. **Full AppDomain Isolation**: More secure but complex
3. **Type Whitelist Only**: Simpler but incomplete

## Implementation Details
- PluginSecurityPolicy for permission enforcement
- PluginSignatureValidator for code signing
- PluginPermissionModel defining allowed operations
- PluginSandbox for execution isolation
- PluginResourceMonitor for limits enforcement

## Permission Categories
- **System**: Can access system services (restricted)
- **Configuration**: Can read/write configuration
- **Profile**: Can create/modify profiles
- **Events**: Can subscribe to events
- **Logging**: Can write logs
- **UI**: Can add UI elements (read-only)
- **Network**: Can initiate network operations (restricted)
- **FileSystem**: Can access specific directories

## Plugin Manifest Permissions
```json
{
  "name": "CustomPlugin",
  "version": "1.0.0",
  "permissions": [
    "system.read_device_list",
    "profile.create",
    "events.subscribe",
    "logging.write"
  ],
  "sandboxed": true,
  "resourceLimits": {
    "maxMemoryMb": 100,
    "maxCpuPercent": 10
  }
}
```
