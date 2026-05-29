/**
 * Webhook Manager Integration for HELIOS v4.0
 * Manages outbound webhooks for event notifications
 * Performance Target: <10ms webhook dispatch
 */

class WebhookManager {
  /**
   * @param {Object} config - Configuration
   * @param {number} config.timeout - Webhook timeout ms (default: 5000)
   * @param {number} config.maxRetries - Max retry attempts (default: 3)
   * @param {boolean} config.queueEnabled - Enable async queue (default: true)
   */
  constructor(config = {}) {
    this.config = {
      timeout: config.timeout || 5000,
      maxRetries: config.maxRetries || 3,
      queueEnabled: config.queueEnabled !== false,
      ...config,
    };

    this.webhooks = new Map();
    this.deliveryQueue = [];
    this.metrics = {
      webhooksRegistered: 0,
      eventsDispatched: 0,
      deliveriesSuccessful: 0,
      deliveriesFailed: 0,
      queueSize: 0,
    };
  }

  /**
   * Register webhook endpoint
   * @param {string} webhookId - Webhook ID
   * @param {Object} webhook - Webhook config
   */
  registerWebhook(webhookId, webhook) {
    if (!webhook.url) {
      throw new Error('Webhook URL is required');
    }

    this.webhooks.set(webhookId, {
      id: webhookId,
      url: webhook.url,
      events: webhook.events || ['*'],
      active: webhook.active !== false,
      headers: webhook.headers || {},
      retryPolicy: webhook.retryPolicy || { maxRetries: this.config.maxRetries },
      created: Date.now(),
      ...webhook,
    });

    this.metrics.webhooksRegistered++;
  }

  /**
   * Dispatch event to matching webhooks
   * @async
   * @param {string} eventType - Event type
   * @param {Object} eventData - Event data
   * @returns {Array} Dispatch results
   */
  async dispatch(eventType, eventData) {
    const results = [];
    const matchingWebhooks = this._findMatchingWebhooks(eventType);

    this.metrics.eventsDispatched++;

    for (const webhook of matchingWebhooks) {
      if (this.config.queueEnabled) {
        this.deliveryQueue.push({ webhook, eventType, eventData });
        this.metrics.queueSize++;
      } else {
        const result = await this._deliverWebhook(webhook, eventType, eventData);
        results.push(result);
      }
    }

    return results;
  }

  /**
   * Process queued webhooks
   * @async
   * @param {number} batchSize - Max webhooks to process
   * @returns {Object} Processing result
   */
  async processQueue(batchSize = 10) {
    const processed = [];
    const batch = this.deliveryQueue.splice(0, batchSize);
    this.metrics.queueSize = this.deliveryQueue.length;

    for (const { webhook, eventType, eventData } of batch) {
      try {
        const result = await this._deliverWebhook(webhook, eventType, eventData);
        processed.push(result);
      } catch (error) {
        processed.push({
          webhookId: webhook.id,
          status: 'error',
          error: error.message,
        });
      }
    }

    return {
      processed: processed.length,
      remaining: this.deliveryQueue.length,
      results: processed,
    };
  }

  /**
   * Get webhook registration
   * @param {string} webhookId - Webhook ID
   * @returns {Object} Webhook config
   */
  getWebhook(webhookId) {
    return this.webhooks.get(webhookId);
  }

  /**
   * Update webhook
   * @param {string} webhookId - Webhook ID
   * @param {Object} updates - Updates to apply
   */
  updateWebhook(webhookId, updates) {
    const webhook = this.webhooks.get(webhookId);
    if (!webhook) return null;

    Object.assign(webhook, updates, { updated: Date.now() });
    return webhook;
  }

  /**
   * Delete webhook
   * @param {string} webhookId - Webhook ID
   */
  deleteWebhook(webhookId) {
    return this.webhooks.delete(webhookId);
  }

  /**
   * Get all webhooks
   * @param {boolean} activeOnly - Only active webhooks
   * @returns {Array} Webhooks
   */
  getAll(activeOnly = false) {
    let webhooks = Array.from(this.webhooks.values());

    if (activeOnly) {
      webhooks = webhooks.filter(w => w.active);
    }

    return webhooks;
  }

  /**
   * Get webhook metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      webhooksRegistered: this.metrics.webhooksRegistered,
      eventsDispatched: this.metrics.eventsDispatched,
      deliveriesSuccessful: this.metrics.deliveriesSuccessful,
      deliveriesFailed: this.metrics.deliveriesFailed,
      queueSize: this.metrics.queueSize,
      successRate: this.metrics.eventsDispatched > 0
        ? ((this.metrics.deliveriesSuccessful / this.metrics.eventsDispatched) * 100).toFixed(2)
        : 0,
    };
  }

  /**
   * Test webhook
   * @async
   * @param {string} webhookId - Webhook ID
   * @returns {Object} Test result
   */
  async testWebhook(webhookId) {
    const webhook = this.webhooks.get(webhookId);
    if (!webhook) {
      return { status: 'error', message: 'Webhook not found' };
    }

    return this._deliverWebhook(webhook, 'test', { test: true, timestamp: Date.now() });
  }

  /**
   * Internal: Find matching webhooks
   * @private
   */
  _findMatchingWebhooks(eventType) {
    return Array.from(this.webhooks.values()).filter(w =>
      w.active && (w.events.includes('*') || w.events.includes(eventType))
    );
  }

  /**
   * Internal: Deliver webhook
   * @private
   */
  async _deliverWebhook(webhook, eventType, eventData, attempt = 1) {
    try {
      const payload = JSON.stringify({
        event: eventType,
        data: eventData,
        timestamp: Date.now(),
      });

      // Simulate HTTP delivery
      const result = {
        webhookId: webhook.id,
        eventType,
        status: 'success',
        statusCode: 200,
        timestamp: Date.now(),
        attempt,
      };

      this.metrics.deliveriesSuccessful++;
      return result;
    } catch (error) {
      if (attempt < webhook.retryPolicy.maxRetries) {
        // Retry logic
        await new Promise(resolve => setTimeout(resolve, 100 * attempt));
        return this._deliverWebhook(webhook, eventType, eventData, attempt + 1);
      }

      this.metrics.deliveriesFailed++;
      return {
        webhookId: webhook.id,
        eventType,
        status: 'failed',
        error: error.message,
        attempt,
      };
    }
  }
}

module.exports = WebhookManager;
