/**
 * HELIOS GUI Dashboard - ENHANCED
 * 8-Tab Interface with error boundaries, state management, alerts
 * Features: Real-time updates, Alert queue, Error boundaries, Caching
 * v7.0 - Production Ready
 */

const { Logger, Validator, EventEmitter, Cache } = require('../utils');

class Widget {
  constructor(id, type, title, data = {}) {
    this.id = id;
    this.type = type;
    this.title = title;
    this.data = data;
    this.lastUpdated = new Date();
    this.errorCount = 0;
    this.isFailed = false;
  }

  update(data) {
    try {
      this.data = { ...this.data, ...data };
      this.lastUpdated = new Date();
      this.isFailed = false;
      return this;
    } catch (error) {
      this.errorCount++;
      this.isFailed = true;
      throw error;
    }
  }

  getStatus() {
    return {
      id: this.id,
      type: this.type,
      title: this.title,
      isFailed: this.isFailed,
      errorCount: this.errorCount,
      lastUpdated: this.lastUpdated,
    };
  }
}

class GUIDashboard extends EventEmitter {
  constructor(config = {}) {
    super();
    this.version = '7.0';
    this.logger = new Logger('GUIDashboard');
    this.tabs = [
      { id: 1, name: 'Overview', data: {}, widgets: [] },
      { id: 2, name: 'Performance', data: {}, widgets: [] },
      { id: 3, name: 'Security', data: {}, widgets: [] },
      { id: 4, name: 'Resources', data: {}, widgets: [] },
      { id: 5, name: 'Alerts', data: {}, widgets: [] },
      { id: 6, name: 'Analytics', data: {}, widgets: [] },
      { id: 7, name: 'Logs', data: {}, widgets: [] },
      { id: 8, name: 'Settings', data: {}, widgets: [] },
    ];
    this.widgets = new Map();
    this.alerts = [];
    this.alertQueue = [];
    this.cache = new Cache(300000); // 5 min cache
    this.theme = config.theme || 'dark';
    this.config = config;
    
    this.logger.info('GUI Dashboard initialized', { version: this.version, theme: this.theme });
    this.emit('dashboard-initialized', { version: this.version });
  }

  getTab(tabId) {
    try {
      const id = Validator.validateNumber(tabId, 'tabId', 1, 8);
      const tab = this.tabs.find(t => t.id === id);
      if (!tab) throw new Error(`Tab ${id} not found`);
      return tab;
    } catch (error) {
      this.logger.error('Failed to get tab', { error: error.message });
      throw error;
    }
  }

  updateTabData(tabId, data) {
    try {
      Validator.validateObject(data, 'data');
      
      const tab = this.getTab(tabId);
      if (!tab) throw new Error(`Tab ${tabId} not found`);

      tab.data = { ...tab.data, ...data, updated: Date.now() };
      
      this.logger.debug(`Tab ${tabId} updated`, { keys: Object.keys(data) });
      this.emit('tab-updated', { tabId, tab });
      
      this.cache.set(`tab_${tabId}`, tab);
      return tab;
    } catch (error) {
      this.logger.error('Failed to update tab data', { error: error.message });
      this.emit('error', { action: 'updateTabData', error });
      throw error;
    }
  }

  addWidget(tabId, widgetConfig) {
    try {
      Validator.validateNumber(tabId, 'tabId', 1, 8);
      Validator.validateObject(widgetConfig, 'widgetConfig', ['type', 'title']);

      const tab = this.getTab(tabId);
      const widgetId = `widget_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
      
      const widget = new Widget(
        widgetId,
        Validator.validateString(widgetConfig.type, 'type'),
        Validator.validateString(widgetConfig.title, 'title'),
        widgetConfig.data || {}
      );

      this.widgets.set(widgetId, widget);
      tab.widgets.push(widgetId);

      this.logger.info(`Widget added to tab ${tabId}`, { widgetId, type: widget.type });
      this.emit('widget-added', { tabId, widget });

      return widget;
    } catch (error) {
      this.logger.error('Failed to add widget', { error: error.message });
      this.emit('error', { action: 'addWidget', error });
      throw error;
    }
  }

  updateWidget(widgetId, data) {
    try {
      const id = Validator.validateString(widgetId, 'widgetId');
      Validator.validateObject(data, 'data');

      const widget = this.widgets.get(id);
      if (!widget) throw new Error(`Widget ${id} not found`);

      widget.update(data);
      
      this.logger.debug(`Widget ${id} updated`, { type: widget.type });
      this.emit('widget-updated', widget);

      return widget;
    } catch (error) {
      this.logger.error('Failed to update widget', { error: error.message });
      this.emit('error', { action: 'updateWidget', error });
      throw error;
    }
  }

  getWidgets(tabId) {
    try {
      const tab = this.getTab(tabId);
      return tab.widgets.map(widgetId => this.widgets.get(widgetId)).filter(w => w !== undefined);
    } catch (error) {
      this.logger.error('Failed to get widgets', { error: error.message });
      return [];
    }
  }

  addAlert(level, message, data = {}) {
    try {
      Validator.validateString(level, 'level');
      Validator.validateString(message, 'message');
      
      if (!['info', 'warning', 'error', 'critical'].includes(level)) {
        throw new Error(`Invalid alert level: ${level}`);
      }

      const alert = {
        id: `alert_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        level,
        message,
        data,
        timestamp: new Date(),
        acknowledged: false,
      };

      this.alerts.push(alert);
      this.alertQueue.push(alert);

      // Keep only last 100 alerts
      if (this.alerts.length > 100) {
        this.alerts.shift();
      }

      this.logger.warn(`Alert added: ${message}`, { level, alertId: alert.id });
      this.emit('alert-added', alert);

      return alert;
    } catch (error) {
      this.logger.error('Failed to add alert', { error: error.message });
      throw error;
    }
  }

  getAlerts(level = null) {
    if (level) {
      return this.alerts.filter(a => a.level === level);
    }
    return this.alerts;
  }

  acknowledgeAlert(alertId) {
    try {
      const id = Validator.validateString(alertId, 'alertId');
      const alert = this.alerts.find(a => a.id === id);
      
      if (!alert) throw new Error(`Alert ${id} not found`);
      
      alert.acknowledged = true;
      this.logger.info(`Alert acknowledged: ${id}`);
      this.emit('alert-acknowledged', alert);
      
      return alert;
    } catch (error) {
      this.logger.error('Failed to acknowledge alert', { error: error.message });
      throw error;
    }
  }

  getAlertQueue() {
    return this.alertQueue;
  }

  clearAlertQueue() {
    const count = this.alertQueue.length;
    this.alertQueue = [];
    this.logger.info(`Alert queue cleared (${count} alerts)`);
    this.emit('alert-queue-cleared', { count });
    return count;
  }

  setTheme(themeName) {
    try {
      const theme = Validator.validateString(themeName, 'theme');
      
      if (!['dark', 'light', 'auto'].includes(theme)) {
        throw new Error(`Invalid theme: ${theme}`);
      }

      this.theme = theme;
      this.logger.info(`Theme changed to ${theme}`);
      this.emit('theme-changed', { theme });

      return this.theme;
    } catch (error) {
      this.logger.error('Failed to set theme', { error: error.message });
      throw error;
    }
  }

  renderDashboard() {
    try {
      const tabsWithWidgets = this.tabs.map(tab => ({
        ...tab,
        widgetsData: this.getWidgets(tab.id).map(w => w.getStatus()),
      }));

      const dashboard = {
        version: this.version,
        theme: this.theme,
        tabs: tabsWithWidgets,
        alertCount: this.alerts.filter(a => !a.acknowledged).length,
        alertQueueSize: this.alertQueue.length,
        rendered: Date.now(),
      };

      this.logger.debug('Dashboard rendered', { tabs: this.tabs.length, widgets: this.widgets.size });
      this.emit('dashboard-rendered', dashboard);

      return dashboard;
    } catch (error) {
      this.logger.error('Failed to render dashboard', { error: error.message });
      this.emit('error', { action: 'renderDashboard', error });
      throw error;
    }
  }

  getWidgetStatus() {
    const statuses = Array.from(this.widgets.values()).map(w => w.getStatus());
    const failed = statuses.filter(s => s.isFailed).length;
    
    return {
      totalWidgets: statuses.length,
      healthyWidgets: statuses.length - failed,
      failedWidgets: failed,
      widgetStatuses: statuses,
    };
  }

  getMetrics() {
    const widgetStatus = this.getWidgetStatus();
    
    return {
      module: 'gui-dashboard',
      version: this.version,
      theme: this.theme,
      tabs: this.tabs.length,
      widgets: this.widgets.size,
      healthyWidgets: widgetStatus.healthyWidgets,
      failedWidgets: widgetStatus.failedWidgets,
      alertCount: this.alerts.length,
      unacknowledgedAlerts: this.alerts.filter(a => !a.acknowledged).length,
      alertQueueSize: this.alertQueue.length,
      cacheSize: this.cache.getSize(),
      logCount: this.logger.getLogs().length,
      timestamp: Date.now(),
    };
  }
}

module.exports = { GUIDashboard, Widget };
