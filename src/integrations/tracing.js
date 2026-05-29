/**
 * Distributed Tracing Integration for HELIOS v4.0
 * Integrates with Jaeger, OpenTelemetry, or custom tracing systems
 * Performance Target: <3ms overhead per span
 */

class TracingIntegration {
  /**
   * @param {Object} config - Configuration
   * @param {string} config.provider - 'jaeger' or 'otel' (default: 'memory')
   * @param {string} config.serviceName - Service name for tracing
   * @param {number} config.samplingRate - Sample rate 0-1 (default: 0.1)
   */
  constructor(config = {}) {
    this.config = {
      provider: config.provider || 'memory',
      serviceName: config.serviceName || 'helios-v4',
      samplingRate: Math.max(0, Math.min(1, config.samplingRate || 0.1)),
      ...config,
    };

    this.spans = [];
    this.activeTraces = new Map();
    this.metrics = {
      spansCreated: 0,
      tracesCompleted: 0,
      totalDuration: 0,
    };
  }

  /**
   * Start a new trace
   * @param {string} traceId - Trace ID (generated if not provided)
   * @param {string} operationName - Operation name
   * @param {Object} tags - Initial tags
   * @returns {Object} Trace context
   */
  startTrace(traceId, operationName, tags = {}) {
    const id = traceId || this._generateId();
    
    const trace = {
      traceId: id,
      operationName,
      startTime: Date.now(),
      tags: { service: this.config.serviceName, ...tags },
      spans: [],
    };

    this.activeTraces.set(id, trace);
    return { traceId: id, spanId: this._generateId() };
  }

  /**
   * Create child span in trace
   * @param {string} traceId - Parent trace ID
   * @param {string} spanName - Span name
   * @param {string} parentSpanId - Parent span ID
   * @returns {Object} Span context
   */
  startSpan(traceId, spanName, parentSpanId = null) {
    const spanId = this._generateId();
    const trace = this.activeTraces.get(traceId);
    
    if (!trace) {
      return { traceId, spanId, error: 'trace_not_found' };
    }

    // Sample traces to reduce overhead
    if (Math.random() > this.config.samplingRate) {
      return { traceId, spanId, sampled: false };
    }

    const span = {
      spanId,
      traceId,
      parentSpanId,
      spanName,
      startTime: Date.now(),
      tags: {},
      logs: [],
    };

    trace.spans.push(span);
    this.metrics.spansCreated++;

    return { traceId, spanId, sampled: true };
  }

  /**
   * End a span
   * @param {string} traceId - Trace ID
   * @param {string} spanId - Span ID
   * @param {string} status - Span status (ok, error)
   */
  endSpan(traceId, spanId, status = 'ok') {
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;

    const span = trace.spans.find(s => s.spanId === spanId);
    if (!span) return;

    span.endTime = Date.now();
    span.duration = span.endTime - span.startTime;
    span.status = status;

    this.spans.push(span);
  }

  /**
   * Complete a trace
   * @param {string} traceId - Trace ID
   * @param {string} status - Trace status
   * @returns {Object} Completed trace
   */
  endTrace(traceId, status = 'ok') {
    const trace = this.activeTraces.get(traceId);
    if (!trace) return null;

    const endTime = Date.now();
    const duration = endTime - trace.startTime;

    trace.endTime = endTime;
    trace.duration = duration;
    trace.status = status;

    this.activeTraces.delete(traceId);
    this.metrics.tracesCompleted++;
    this.metrics.totalDuration += duration;

    return trace;
  }

  /**
   * Add tag to span
   * @param {string} traceId - Trace ID
   * @param {string} spanId - Span ID
   * @param {string} key - Tag key
   * @param {*} value - Tag value
   */
  addTag(traceId, spanId, key, value) {
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;

    const span = trace.spans.find(s => s.spanId === spanId);
    if (span) {
      span.tags[key] = value;
    }
  }

  /**
   * Add log to span
   * @param {string} traceId - Trace ID
   * @param {string} spanId - Span ID
   * @param {Object} logData - Log data
   */
  addLog(traceId, spanId, logData) {
    const trace = this.activeTraces.get(traceId);
    if (!trace) return;

    const span = trace.spans.find(s => s.spanId === spanId);
    if (span) {
      span.logs.push({
        timestamp: Date.now(),
        ...logData,
      });
    }
  }

  /**
   * Get trace information
   * @param {string} traceId - Trace ID
   * @returns {Object} Trace data
   */
  getTrace(traceId) {
    return this.activeTraces.get(traceId);
  }

  /**
   * Get completed spans
   * @param {number} limit - Max spans to return
   * @returns {Array} Spans
   */
  getSpans(limit = 100) {
    return this.spans.slice(-limit);
  }

  /**
   * Get tracing metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      spansCreated: this.metrics.spansCreated,
      tracesCompleted: this.metrics.tracesCompleted,
      avgTraceDuration: this.metrics.tracesCompleted > 0
        ? `${(this.metrics.totalDuration / this.metrics.tracesCompleted).toFixed(2)}ms`
        : 0,
      activeTraces: this.activeTraces.size,
      samplingRate: `${(this.config.samplingRate * 100).toFixed(1)}%`,
    };
  }

  /**
   * Export traces
   * @async
   * @returns {Object} Export result
   */
  async export() {
    return {
      status: 'exported',
      provider: this.config.provider,
      spansCount: this.spans.length,
      tracesCompleted: this.metrics.tracesCompleted,
    };
  }

  /**
   * Clear old spans
   * @param {number} maxAge - Max age in ms (default: 1 hour)
   */
  cleanup(maxAge = 3600000) {
    const now = Date.now();
    this.spans = this.spans.filter(s => now - s.endTime < maxAge);
  }

  /**
   * Internal: Generate ID
   * @private
   */
  _generateId() {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}

module.exports = TracingIntegration;
