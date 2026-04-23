# ADR-005: Integrated Performance Profiling and Monitoring

## Status
Accepted

## Context
Monado Blade must perform efficiently across various hardware configurations. Performance must be:
- Measurable and baseline-tracked
- Visible during development and operations
- Identifiable at specific operation level
- Actionable for optimization

## Problem
How to measure and track performance without degrading it significantly?

## Decision
Implement integrated profiling with conditional collection:
1. Define performance measurement interfaces
2. Collect metrics at operation boundaries (services, state transitions)
3. Store metrics in-memory with configurable history
4. Support querying and analysis of metrics
5. Implement profiling levels (Off, Summary, Detailed, Full)
6. Create performance analysis tools for batch analysis

## Consequences

### Positive
- Performance data available for optimization decisions
- Bottlenecks identified quickly
- Historical comparison shows regression detection
- Minimal performance impact when disabled
- Flexible profiling levels for different scenarios

### Negative
- Memory overhead for storing metrics (even when off)
- Code must be instrumented for profiling
- Analysis tools needed to make use of data
- Debugging complex performance issues still difficult

## Alternatives Considered

1. **No Built-in Profiling**: Use external profilers only
   - External profilers not always practical in deployment
   - Misses application-level insights

2. **Always-On Detailed Profiling**: Comprehensive but expensive
   - Significant performance and memory overhead
   - Not practical for production

3. **Manual Profiling Points**: Developer adds profiling code as needed
   - Inconsistent and incomplete coverage

## Implementation Details

### Profiling Levels
- **Off (0)**: No metric collection
- **Summary (1)**: Operation start/end times only
- **Detailed (2)**: Summary + memory usage + key events
- **Full (3)**: Detailed + CPU usage + detailed memory allocation

### Metrics Collected
- Operation execution time (milliseconds)
- Memory usage at operation start/end
- CPU usage percentage
- Cache hit/miss rates
- Thread count
- Garbage collection events

### Metric Storage
- Circular buffer of last 1000 operations
- Configurable retention period
- Export to CSV for analysis

### Performance Targets
- Boot sequence: < 5 seconds
- Profile application: < 2 seconds per profile
- Update sequence: < 30 seconds (excluding network)
- USB detection: < 100ms
- State transitions: < 10ms

## Usage Example
```csharp
using var profiler = new OperationProfiler("USBDetection", ProfilingLevel.Summary);
// USB detection logic
profiler.RecordEvent("DeviceFound");
```
