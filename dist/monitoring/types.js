"use strict";
/**
 * Core types and interfaces for the Monado Blade monitoring system
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.DegradationMode = exports.HealthStatus = exports.CircuitBreakerState = void 0;
var CircuitBreakerState;
(function (CircuitBreakerState) {
    CircuitBreakerState["CLOSED"] = "CLOSED";
    CircuitBreakerState["OPEN"] = "OPEN";
    CircuitBreakerState["HALF_OPEN"] = "HALF_OPEN";
})(CircuitBreakerState || (exports.CircuitBreakerState = CircuitBreakerState = {}));
var HealthStatus;
(function (HealthStatus) {
    HealthStatus["HEALTHY"] = "HEALTHY";
    HealthStatus["DEGRADED"] = "DEGRADED";
    HealthStatus["UNHEALTHY"] = "UNHEALTHY";
})(HealthStatus || (exports.HealthStatus = HealthStatus = {}));
var DegradationMode;
(function (DegradationMode) {
    DegradationMode["NORMAL"] = "NORMAL";
    DegradationMode["REDUCED"] = "REDUCED";
    DegradationMode["CRITICAL"] = "CRITICAL";
})(DegradationMode || (exports.DegradationMode = DegradationMode = {}));
//# sourceMappingURL=types.js.map