# ADR-006: USB Device Abstraction Layer

## Status
Accepted

## Context
Monado Blade needs to support multiple USB protocols and device types. Direct dependency on specific USB APIs makes code brittle and hard to test.

## Problem
How to support multiple USB device types and protocols while keeping code testable?

## Decision
Create abstraction layer over USB communication:
1. Define IUSBDevice interface for device abstraction
2. Define IUSBProtocol interface for protocol abstraction
3. Implement protocol handlers for MTP, UMS, and custom protocols
4. Support device hot-plug events through observer pattern
5. Abstract USB details from higher-level services
6. Enable mock implementations for testing

## Consequences

### Positive
- Easy to test USB logic with mock devices
- Can support new USB devices/protocols by implementing interface
- Core business logic independent of USB specifics
- Device detection logic centralized

### Negative
- Abstraction layer adds indirection
- Must keep interfaces aligned with implementations
- Not all USB features may be abstractable

## Alternatives Considered

1. **Direct USB API Usage**: Simpler but tightly coupled to USB
2. **Third-party USB Library**: Adds dependency but might be premature

## Implementation Details
- IUSBDevice in Core/Services/Interfaces/USB
- Implementations in Core/Services/Implementation/USB
- Factory pattern for creating device instances
- Event bus for device hot-plug notification
