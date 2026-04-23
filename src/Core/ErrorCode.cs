namespace MonadoBlade.Core;

/// <summary>
/// Unified error code enumeration. All systems use these codes for consistency.
/// Organized by range: Core (0-999), AIHub (1000-1999), SDK (2000-2999), 
/// Orchestration (3000-3999), UI (4000-4999), Security (5000-5999).
/// </summary>
public enum ErrorCode
{
    // Core errors (0-999)
    Success = 0,
    Unknown = 1,
    InvalidArgument = 2,
    InvalidOperation = 3,
    NotFound = 4,
    AlreadyExists = 5,
    Timeout = 6,
    CommunicationError = 7,
    ConfigurationError = 8,
    DependencyError = 9,
    
    // Validation errors (100-199)
    ValidationFailed = 100,
    RequiredFieldMissing = 101,
    InvalidFormat = 102,
    OutOfRange = 103,
    ConstraintViolation = 104,
    
    // Caching errors (200-299)
    CacheError = 200,
    CacheInvalidated = 201,
    CacheMiss = 202,
    
    // Logging/Telemetry errors (300-399)
    LoggingError = 300,
    TelemetryError = 301,
    
    // Concurrency errors (400-499)
    DeadlockDetected = 400,
    RaceConditionDetected = 401,
    ConcurrencyViolation = 402,
    
    // Resource errors (500-599)
    ResourceExhausted = 500,
    ResourceUnavailable = 501,
    MemoryError = 502,
    
    // AI Hub errors (1000-1999)
    AIProviderError = 1000,
    AIProviderUnavailable = 1001,
    AIProviderRateLimited = 1002,
    AIInferenceError = 1003,
    AIModelNotFound = 1004,
    AIConfigurationError = 1005,
    
    // SDK errors (2000-2999)
    SDKInitializationError = 2000,
    SDKProviderError = 2001,
    SDKNotSupported = 2002,
    SDKCallError = 2003,
    SDKAuthenticationError = 2004,
    
    // Orchestration errors (3000-3999)
    VMBootError = 3000,
    VMShutdownError = 3001,
    VMStateError = 3002,
    LoadBalancingError = 3003,
    HealthCheckError = 3004,
    OrchestrationConflict = 3005,
    
    // UI/Automation errors (4000-4999)
    UIRenderError = 4000,
    UIInputError = 4001,
    DashboardError = 4002,
    AutomationError = 4003,
    
    // Security errors (5000-5999)
    EncryptionError = 5000,
    DecryptionError = 5001,
    AuthenticationFailed = 5002,
    AuthorizationFailed = 5003,
    TPMError = 5004,
    CertificateError = 5005,
    InvalidSignature = 5006,
}
