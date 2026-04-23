# Troubleshooting Knowledge Base

## Quick Diagnostics

### Collect System Information

```powershell
# Gather diagnostic data
$diag = @{
    OS = [Environment]::OSVersion
    Memory = (Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
    Disk = Get-Volume | Select-Object DriveLetter, Capacity, SizeRemaining
    USB = Get-PnpDevice -Class USB
    MonadoLogs = Get-ChildItem -Path "$env:ProgramData\MonadoBlade\Logs" -Recurse
}

# Export for analysis
$diag | ConvertTo-Json | Out-File diagnostic-report.json
```

### Enable Debug Logging

Modify configuration:
```json
{
  "logging": {
    "level": "Debug",
    "console": true,
    "file": "monado-debug.log"
  }
}
```

## Common Issues and Solutions

### USB Device Not Detected

**Symptoms**: 
- USB device not appearing in device list
- "No USB device found" error

**Diagnosis**:
1. Check physical connection
2. Verify driver installation: `pnputil /enum-devices`
3. Check USB port: Try different port
4. Review logs for USBException

**Solutions**:
1. Restart USB service: `Restart-Service -Name "usbhub"`
2. Reinstall USB drivers
3. Update device drivers
4. Check USB cable and port for issues

**Related**: ADR-006 (USB Abstraction)

### Boot Sequence Fails

**Symptoms**:
- Boot state stuck or rapidly cycling
- "Invalid state transition" error

**Diagnosis**:
1. Check current boot state
2. Verify state machine configuration
3. Review boot sequence timeout settings

**Solutions**:
1. Increase timeout in profile:
   ```json
   {
     "bootSequence": [{
       "stage": "initialization",
       "timeout": 20000
     }]
   }
   ```
2. Check device connectivity
3. Review detailed logs for specific failure point

**Related**: ADR-002 (State Machine), ADR-005 (Performance)

### Update Fails Mid-Process

**Symptoms**:
- Update stops at random point
- Partial update applied
- Cannot recover

**Diagnosis**:
1. Check network connectivity
2. Verify USB connection stability
3. Review update logs for error details

**Solutions**:
1. Attempt rollback: 
   ```csharp
   await updateService.RollbackAsync(backupId);
   ```
2. If rollback unavailable, restore from backup
3. Retry update with more verbose logging

**Related**: ADR-012 (Update Rollback)

### High Memory Usage

**Symptoms**:
- Application consuming excessive memory
- System becomes unresponsive

**Diagnosis**:
1. Check profiling metrics:
   ```csharp
   var metrics = profiler.GetMetrics();
   Console.WriteLine($"Memory: {metrics.PeakMemoryMb}MB");
   ```
2. Review cache statistics
3. Check for memory leaks in logs

**Solutions**:
1. Reduce cache TTL:
   ```json
   {
     "cache": {
       "deviceListTtlMs": 5000,
       "profileTtlMs": 10000
     }
   }
   ```
2. Disable detailed profiling if enabled
3. Review for plugin memory leaks

**Related**: ADR-005 (Performance), ADR-013 (Caching)

### Slow Boot Performance

**Symptoms**:
- Boot takes longer than expected
- User reports sluggish response

**Diagnosis**:
1. Enable performance profiling:
   ```json
   {
     "profiling": {
       "level": "Summary"
     }
   }
   ```
2. Review operation timings
3. Identify bottleneck operations

**Solutions**:
1. Optimize identified bottleneck
2. Increase cache validity periods
3. Profile specific operations
4. Review system resources (CPU, disk)

**Related**: ADR-005 (Performance Profiling)

### Plugin Not Loading

**Symptoms**:
- Plugin missing from installed list
- "Plugin not found" error

**Diagnosis**:
1. Verify manifest.json exists and valid
2. Check plugin signature
3. Verify permissions granted

**Solutions**:
1. Validate manifest:
   ```powershell
   monado-cli plugin validate plugin.zip
   ```
2. Check plugin logs for errors
3. Verify plugin assembly matches manifest
4. Grant required permissions

**Related**: ADR-015 (Plugin Security)

### Configuration Not Applied

**Symptoms**:
- Configuration changes don't take effect
- Old settings still used

**Diagnosis**:
1. Verify configuration file syntax (JSON)
2. Check configuration precedence
3. Review logs for parsing errors

**Solutions**:
1. Restart service to reload configuration
2. Check file permissions and location
3. Verify JSON syntax:
   ```powershell
   Get-Content config.json | ConvertFrom-Json
   ```

**Related**: ADR-008 (Configuration)

### Authentication Fails

**Symptoms**:
- Cannot authenticate with credentials
- "Authentication failed" error

**Diagnosis**:
1. Verify credentials
2. Check authentication service logs
3. Verify account has required permissions

**Solutions**:
1. Reset password through admin
2. Grant necessary permissions
3. Check system clock (for token validation)
4. Review security event logs

**Related**: ADR-004 (Security)

## Performance Tuning

### Reduce Boot Time

1. **Profile optimization**:
   ```json
   {
     "bootSequence": [{
       "stage": "initialization",
       "timeout": 5000,
       "parallel": true
     }]
   }
   ```

2. **Cache tuning**:
   ```json
   {
     "cache": {
       "enabled": true,
       "ttlMs": 30000
     }
   }
   ```

3. **Service optimization**: Profile and optimize bottleneck services

### Reduce Memory Usage

1. Disable profiling when not needed
2. Reduce cache sizes
3. Monitor plugin memory usage
4. Review log retention policies

### Improve Responsiveness

1. Enable async operations
2. Reduce operation timeouts for faster failure detection
3. Implement proper cancellation

## Security Hardening

### Audit Trail

```csharp
// Enable audit logging
config.Set("logging.audit.enabled", true);
config.Set("logging.audit.level", "Verbose");

// Review audit logs for suspicious activity
```

### Plugin Security

1. Only install signed plugins
2. Review plugin permissions before installation
3. Monitor plugin resource usage
4. Remove unused plugins

### Configuration Security

1. Store sensitive settings in secure locations
2. Use environment variables for secrets
3. Restrict configuration file access
4. Review configuration changes regularly

## Log Analysis

### Query Logs

```powershell
# Find errors
Get-Content logs/monado.log | 
  Select-String "ERROR|FATAL" |
  Sort-Object -Descending |
  Select-Object -First 20

# Find specific events
Get-Content logs/monado.log |
  Select-String "BootStateChanged" |
  ConvertFrom-Json
```

### Performance Analysis

```csharp
// Export performance data
var metrics = profiler.GetMetrics();
var csv = metrics.ToCsv();
File-WriteAllText("performance.csv", csv);
```

## Advanced Debugging

### Attach Debugger

```csharp
if (config.Get<bool>("debug.breakOnStart"))
{
    Debugger.Break();
}
```

### Enable Verbose Logging

```json
{
  "logging": {
    "level": "Debug",
    "console": true,
    "profiling": "Full"
  }
}
```

### Collect ETW Traces (Windows)

```powershell
# Start trace
New-NetEventSession -Name "MonadoTrace" -LocalFilePath "trace.etl"
Add-NetEventProvider -SessionName "MonadoTrace" -Name "Monado Blade"
Start-NetEventSession -SessionName "MonadoTrace"

# Stop trace
Stop-NetEventSession -SessionName "MonadoTrace"

# Analyze
Get-NetEventPacket -InputFile trace.etl
```

## Getting Help

1. **Check Logs**: Most issues evident in logs
2. **Run Diagnostics**: Collect system information
3. **Review Documentation**: Check related ADRs
4. **Search Issues**: GitHub issues may have solutions
5. **Contact Support**: Provide diagnostic report and logs

## Related Documentation

- ADR-004: Security Approach
- ADR-005: Performance Profiling
- ADR-009: Logging Strategy
- DEPLOYMENT_GUIDE.md: Deployment troubleshooting
