# ADR-007: Event-Driven Architecture for Decoupling Services

## Status
Accepted

## Context
Services need to react to significant events (USB detected, state changed, update completed) without tight coupling.

## Problem
How to allow services to react to events without creating dependencies between them?

## Decision
Implement centralized event bus:
1. Define domain events as specific event types
2. Create IEventBus interface for pub/sub
3. Services subscribe to relevant events
4. Services publish events when significant changes occur
5. Event handling is asynchronous by default
6. Event handlers are ordered and can return results

## Consequences

### Positive
- Services loosely coupled through events
- Easy to add event handlers without modifying publishers
- Clear audit trail of significant events
- Can test event handling in isolation

### Negative
- Harder to trace execution flow
- Event handlers must be registered explicitly
- Asynchronous handling increases complexity
- Silent failures if event handlers not registered

## Alternatives Considered

1. **Direct Method Calls**: Simpler but creates tight coupling
2. **Callbacks**: More direct than event bus but still coupled
3. **Message Queue**: More complex than needed for in-process

## Implementation Details
- IEventBus interface in Core/Events
- DomainEvent base class for all events
- Specific event types in Core/Events/DomainEvents
- Event handlers implementing IEventHandler<TEvent>

## Domain Event Types
- USBDeviceConnected
- USBDeviceDisconnected
- BootStateChanged
- UpdateStarted
- UpdateCompleted
- ProfileApplied
- ErrorOccurred
- ConfigurationChanged
