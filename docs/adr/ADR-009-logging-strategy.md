# ADR-009: Structured Logging with Context Preservation

## Status
Accepted

## Context
Troubleshooting production issues requires rich, queryable logs. Simple string logging is insufficient.

## Problem
How to implement logging that supports both human readability and programmatic analysis?

## Decision
Implement structured logging:
1. Use structured logging library (Serilog)
2. Define structured log events with typed properties
3. Include correlation IDs for tracing related operations
4. Log at appropriate levels (Debug, Info, Warning, Error, Fatal)
5. Separate audit logs from operation logs
6. Store logs in queryable format (JSON)
7. Implement log aggregation for multi-instance scenarios

## Consequences

### Positive
- Logs are searchable and queryable
- Correlation IDs enable tracing across services
- Rich context available for troubleshooting
- Human-readable and machine-readable simultaneously
- Easy to alert on specific conditions

### Negative
- Structured logging requires discipline
- More verbose than string concatenation
- Storage and bandwidth overhead for detailed logs
- Learning curve for structured logging concepts

## Alternatives Considered

1. **Unstructured String Logging**: Simpler but hard to query
2. **Application Logging Only**: Missing system-level events

## Implementation Details
- Serilog for structured logging
- Correlation ID propagated through AsyncLocal
- Log levels mapped to severity
- Enrichers add context (machine, process, version)
- Sinks configured for file and console output

## Log Levels and Usage
- **Debug**: Detailed flow information, variable values
- **Information**: Significant events (boot started, update completed)
- **Warning**: Recoverable issues (retry attempted, device disconnected)
- **Error**: Failure with impact (operation failed, retry exhausted)
- **Fatal**: System failure (cannot recover, shutdown)
