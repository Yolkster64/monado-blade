/**
 * Error Handler Utility for HELIOS v4.0
 * Standardized error handling, classification, and response formatting
 * Used by: all modules
 */

/**
 * Custom error class for HELIOS
 */
class HeliosError extends Error {
  constructor(message, code = 'UNKNOWN', statusCode = 500, context = {}) {
    super(message);
    this.name = 'HeliosError';
    this.code = code;
    this.statusCode = statusCode;
    this.context = context;
    this.timestamp = Date.now();
  }

  toJSON() {
    return {
      error: this.message,
      code: this.code,
      statusCode: this.statusCode,
      timestamp: this.timestamp,
      context: this.context,
    };
  }
}

/**
 * Error classifier
 */
const ErrorClassifier = {
  CACHE_ERROR: { code: 'CACHE_ERROR', statusCode: 503 },
  DB_ERROR: { code: 'DB_ERROR', statusCode: 503 },
  VALIDATION_ERROR: { code: 'VALIDATION_ERROR', statusCode: 400 },
  NOT_FOUND: { code: 'NOT_FOUND', statusCode: 404 },
  UNAUTHORIZED: { code: 'UNAUTHORIZED', statusCode: 401 },
  TIMEOUT: { code: 'TIMEOUT', statusCode: 504 },
  INTERNAL_ERROR: { code: 'INTERNAL_ERROR', statusCode: 500 },
};

/**
 * Classify error and create standardized response
 * @param {Error} error - Error to classify
 * @param {string} context - Context where error occurred
 * @returns {Object} Standardized error response
 */
function classifyError(error, context = '') {
  if (error instanceof HeliosError) {
    return error.toJSON();
  }

  let classification = ErrorClassifier.INTERNAL_ERROR;

  if (error.message.includes('timeout')) {
    classification = ErrorClassifier.TIMEOUT;
  } else if (error.message.includes('not found')) {
    classification = ErrorClassifier.NOT_FOUND;
  } else if (error.message.includes('cache')) {
    classification = ErrorClassifier.CACHE_ERROR;
  } else if (error.message.includes('database') || error.message.includes('query')) {
    classification = ErrorClassifier.DB_ERROR;
  }

  return {
    error: error.message,
    code: classification.code,
    statusCode: classification.statusCode,
    context,
    timestamp: Date.now(),
  };
}

/**
 * Safe error handler wrapper
 * @param {Function} fn - Async function to wrap
 * @returns {Function} Wrapped function with error handling
 */
function safeHandler(fn) {
  return async (...args) => {
    try {
      return await fn(...args);
    } catch (error) {
      const classified = classifyError(error, fn.name);
      throw new HeliosError(classified.error, classified.code, classified.statusCode, { context: fn.name });
    }
  };
}

module.exports = {
  HeliosError,
  ErrorClassifier,
  classifyError,
  safeHandler,
};
