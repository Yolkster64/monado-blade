# ADR-003: Structured Error Handling with Custom Exception Hierarchy

## Status
Accepted

## Context
Monado Blade processes critical operations (USB detection, firmware updates, profile applications) that can fail in various ways. Error handling must be:
- Specific and actionable
- Recoverable with clear retry strategies
- Loggable with full context
- User-friendly with helpful messages

## Problem
How to handle errors consistently across services while providing enough context for recovery and debugging?

## Decision
Implement hierarchical exception system with specialized exception types:
1. Create MonadoBladeException base class for all domain exceptions
2. Define specific exception types: USBException, BootException, UpdateException, etc.
3. Include error codes for programmatic handling
4. Include context data in exceptions for debugging
5. Create exception handlers for each exception type
6. Wrap lower-level exceptions with domain context

## Consequences

### Positive
- Specific catch blocks handle specific errors
- Error codes enable programmatic recovery
- Context data aids troubleshooting
- Consistent error handling across all services
- Clear distinction between recoverable and fatal errors

### Negative
- Need to define and maintain exception hierarchy
- Must be careful not to lose original exception stack trace
- Exception proliferation can make code verbose

## Alternatives Considered

1. **Generic Exception with Error Codes Only**: Simpler but less type-safe
2. **Result<T> Pattern**: Functional approach, used for some operations
3. **Error Codes Only**: No exceptions, just return codes
   - Hard to propagate error context
   - Leads to error code checking everywhere

## Implementation Details
- Base exception: MonadoBladeException in Core/Exceptions
- Domain exceptions: USBException, BootException, UpdateException, etc.
- Error codes defined as enums in Core/Exceptions/ErrorCodes
- Context includes: service name, timestamp, involved component, suggested action

## Error Code Ranges
- 1000-1999: USB Management errors
- 2000-2999: Boot Process errors
- 3000-3999: Update Process errors
- 4000-4999: Profile Management errors
- 5000-5999: Security/Authentication errors
- 9000-9999: System/Infrastructure errors
