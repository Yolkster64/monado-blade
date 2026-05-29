# HELIOS Platform v3.6.0 - Frequently Asked Questions

**Version**: 3.6.0

## Installation & Setup

**Q: What are the minimum system requirements?**
A: Windows 11 Pro or Server 2022+, 4GB RAM, 10GB disk space, .NET 8.0, and stable internet connection.

**Q: Can I install HELIOS on Windows 10?**
A: HELIOS v3.6.0 requires Windows 11 Pro or Server 2022+. Consider upgrading or using previous versions.

**Q: How long does installation take?**
A: Typical installation takes 3-5 minutes depending on disk speed. First startup takes additional 1-2 minutes for initialization.

**Q: Can I run multiple HELIOS instances?**
A: Yes, you can run multiple instances with different configuration, but they'll share the same database by default. For isolation, use separate SQL Server databases.

## Cloud Synchronization

**Q: Which cloud providers are supported?**
A: v3.6.0 supports OneDrive, Azure Blob Storage, AWS S3, and Google Drive.

**Q: How often does automatic sync occur?**
A: Default is every 5 minutes, configurable from 1 minute to 1 hour in Settings > Cloud Sync > Options.

**Q: What happens if sync fails?**
A: Failed syncs are automatically retried with exponential backoff. Check Dashboard > Cloud Sync > Logs for details.

**Q: Can I encrypt cloud data?**
A: Yes, AES-256 encryption is available and recommended for all cloud sync operations.

**Q: How much storage do I need?**
A: Cloud storage requirement matches local data size plus 20% overhead for sync cache and metadata.

## Plugins

**Q: Are plugins safe?**
A: Plugins run in isolated sandboxes with configurable permissions. Always review permissions before installing from untrusted sources.

**Q: Can I develop my own plugins?**
A: Yes, see FEATURES_GUIDE.md > Plugin System for development instructions and examples.

**Q: How do I update a plugin?**
A: Go to Extensions > Installed Plugins > [Plugin] > "Update" appears when available.

**Q: Can plugins access my cloud data?**
A: Only if explicitly granted permission. Always review plugin permissions.

## Dashboard & UI

**Q: Why can't I access the dashboard?**
A: Verify HELIOS service is running and port 8080 is not blocked by firewall. Try https://localhost:8080.

**Q: Is the dashboard secure?**
A: Yes, dashboard requires authentication and uses HTTPS with TLS 1.3+.

**Q: Can I customize the dashboard?**
A: Yes, create custom views in Dashboard > + Add View > select view type and configure.

**Q: How do I enable dark mode?**
A: Settings > Appearance > Theme > select "Dark" or "Auto" to follow system setting.

## AI/ML Features

**Q: What ML frameworks are supported?**
A: TensorFlow, PyTorch, ONNX, and custom models via IInferenceEngine interface.

**Q: Can I use my own trained models?**
A: Yes, register models via ML Services > Model Management > Register Model.

**Q: How accurate are predictions?**
A: Accuracy depends on model and training data. Check model metadata for accuracy metrics.

## Performance & Optimization

**Q: What's the typical dashboard latency?**
A: <50ms for page loads, 60fps for real-time updates with <1second data refresh.

**Q: How much CPU/Memory does HELIOS use?**
A: Core service: 2-5% CPU, 200-400MB RAM. Varies with plugins and workload.

**Q: Can I optimize performance further?**
A: Yes, see DEPLOYMENT.md > Performance Tuning for CPU, Memory, Disk, and Network optimization.

## Security

**Q: Is HELIOS suitable for production?**
A: Yes, v3.6.0 is production-ready with enterprise-grade security, monitoring, and support.

**Q: What encryption algorithms are used?**
A: AES-256 for data, PBKDF2 for passwords, TLS 1.3+ for transport.

**Q: Does HELIOS support Active Directory?**
A: Yes, HELIOS integrates with Windows Active Directory for authentication.

**Q: How are credentials stored?**
A: Cloud provider credentials are encrypted at rest and never logged. Use Windows Credential Manager or Azure Key Vault.

## Troubleshooting

**Q: HELIOS service won't start. What do I do?**
A: Check Windows Event Viewer for errors. Verify appsettings.json is valid JSON. Check database connectivity.

**Q: My plugins are crashing. How do I debug?**
A: Check Dashboard > Plugins > [Plugin] > Logs for error messages. Enable Debug logging in Settings > Logging > Level.

**Q: Cloud sync is very slow. How can I speed it up?**
A: Check network bandwidth, reduce sync interval, enable compression, or reduce excluded paths.

**Q: Dashboard shows "Unhealthy" status. What does it mean?**
A: Click on status to see details. Common causes: database disconnect, disk full, high CPU/memory. See logs for specifics.

## Updates & Maintenance

**Q: How often are updates released?**
A: Major versions quarterly, minor/patch as needed. Check GitHub releases for latest.

**Q: Can I schedule updates automatically?**
A: Currently manual, but you can automate via PowerShell scripts. Automatic updates planned for v3.7.0.

**Q: What's the backup strategy?**
A: Daily full backup of database + config recommended. Cloud providers provide redundancy.

**Q: How do I rollback to previous version?**
A: Restore application files and database from backup. See DEPLOYMENT.md > Rollback Procedures.

---

**Can't find your answer? Visit [GitHub Discussions](https://discussions.github.com/M0nado/helios-platform)**
