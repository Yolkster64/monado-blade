# ADR-001: Service-Oriented Architecture with Dependency Injection

## Status
Accepted

## Context
Monado Blade needs to manage multiple complex services (USB management, boot sequencing, profile management, update management) that need to be loosely coupled, testable, and extensible. Different boot stages require different service configurations.

## Problem
How should services be organized and instantiated to support:
- Loose coupling between services
- Easy testing and mocking
- Stage-specific service configurations
- Clean dependency management
- Extensibility for new services

## Decision
Implement a Service-Oriented Architecture with Dependency Injection pattern:
1. Define service interfaces in dedicated interfaces namespace
2. Implement services as separate classes with constructor injection
3. Create ServiceContainer for managing service lifecycle
4. Use factory pattern for stage-specific configurations
5. Support both registration and resolution of services

## Consequences

### Positive
- Services are fully testable through dependency injection
- New services can be added without modifying existing code
- Clear separation of concerns and responsibilities
- Services can be easily replaced with mock implementations
- Stage-specific configurations are cleanly managed

### Negative
- Requires setup ceremony for service container
- More classes than monolithic approach
- Need to maintain interface contracts across services
- Learning curve for new developers

## Alternatives Considered

1. **Monolithic Implementation**: All logic in single class
   - Simpler initially but harder to test and maintain

2. **Static Service Locator**: Global service registry accessed statically
   - Avoids constructor injection but creates hidden dependencies
   - Harder to test as dependencies aren't explicit

3. **Factory Pattern Only**: No formal service container
   - Less standardized, harder to manage complex dependency graphs

## Implementation Details
- ServiceContainer class located in Core/DependencyInjection
- Interface definitions in Core/Services/Interfaces
- Implementation in Core/Services/Implementation
- Boot stages configure services in stage-specific containers

## References
- Microsoft Dependency Injection documentation
- Dependency Inversion Principle (SOLID)
