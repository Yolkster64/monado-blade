# Deployment Guide - Monado Blade v2.4.0

**Complete guide to deploying Monado Blade in production environments**

*Read time: 30-40 minutes | Skill level: Advanced*

## Table of Contents

1. [Quick Start (5 minutes)](#quick-start-5-minutes)
2. [Single Provider Setup](#single-provider-setup)
3. [Multi-Provider Setup](#multi-provider-setup)
4. [Local Hyper-V Deployment](#local-hyper-v-deployment)
5. [Cloud Deployments](#cloud-deployments)
6. [Production Setup](#production-setup)
7. [Monitoring & Alerting](#monitoring--alerting)
8. [Performance Tuning](#performance-tuning)
9. [Troubleshooting](#troubleshooting)

---

## Quick Start (5 minutes)

### The Absolute Minimum Setup

```powershell
# 1. Clone the repository
git clone https://github.com/company/monado-blade.git
cd monado-blade

# 2. Set environment variables
$env:OPENAI_API_KEY = "your-key-here"
$env:PROVIDER_STRATEGY = "balanced"

# 3. Build
dotnet build

# 4. Run
dotnet run --project src/Monado.Server

# 5. Test
$response = Invoke-WebRequest -Uri "http://localhost:8080/api/inference" `
  -Method POST `
  -Body '{"prompt":"Hello","model":"gpt-3.5-turbo"}' `
  -ContentType "application/json"

Write-Host $response.Content
```

**That's it!** You now have a running Monado instance.

---

## Single Provider Setup

### Scenario: Just Starting Out

You have one API key and want to get running quickly.

### Architecture

```
┌──────────────┐
│  Your App    │
└──────┬───────┘
       │
       ▼
┌────────────────────┐
│  SmartRouter       │
│  (SingleProvider)  │
└──────┬─────────────┘
       │
       ▼
┌────────────────────┐
│  OpenAI Provider   │
│  (gpt-3.5-turbo)   │
└────────────────────┘
```

### Configuration

Create `appsettings.json`:

```json
{
  "Monado": {
    "Service": {
      "Name": "MonadoSingleProvider",
      "Environment": "Production",
      "LogLevel": "Information"
    },
    "Providers": [
      {
        "Name": "OpenAI",
        "Type": "OpenAI",
        "Enabled": true,
        "Configuration": {
          "ApiKey": "${OPENAI_API_KEY}",
          "Model": "gpt-3.5-turbo",
          "Endpoint": "https://api.openai.com/v1"
        }
      }
    ],
    "Routing": {
      "Strategy": "passthrough",
      "DefaultProvider": "OpenAI",
      "EnableMetrics": true
    },
    "Cache": {
      "Enabled": true,
      "Type": "Memory",
      "Duration": 3600
    },
    "Logging": {
      "Console": true,
      "File": "/var/log/monado/app.log",
      "Level": "Information"
    }
  }
}
```

### Startup Code

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Monado services
builder.Services.AddMonadoBlade(builder.Configuration);

// Add single OpenAI provider
builder.Services.AddSingleton<IProviderAdapter>(sp =>
{
    var context = sp.GetRequiredService<IServiceContext>();
    var provider = new OpenAIProvider(context);
    provider.InitializeAsync(context).GetAwaiter().GetResult();
    return provider;
});

// Add router
builder.Services.AddSingleton<ISmartRouter, PassthroughRouter>();

var app = builder.Build();

app.UseMonadoBlade();
app.MapInferenceEndpoints();

await app.RunAsync();
```

### Health Check

```csharp
[HttpGet("health")]
public async Task<IActionResult> HealthCheck()
{
    var provider = HttpContext.RequestServices
        .GetRequiredService<IProviderAdapter>();
    
    var health = await provider.GetHealthAsync();
    
    if (health.State == HealthState.Healthy)
        return Ok(new { status = "healthy", provider = provider.ProviderName });
    
    return StatusCode(503, new { 
        status = "unhealthy", 
        reason = health.Reason 
    });
}
```

### Testing

```bash
# Test inference
curl -X POST http://localhost:8080/api/inference \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [{"role": "user", "content": "What is 2+2?"}],
    "model": "gpt-3.5-turbo",
    "max_tokens": 10
  }'

# Check health
curl http://localhost:8080/health

# Check metrics
curl http://localhost:8080/metrics
```

---

## Multi-Provider Setup

### Scenario: Production Environment

Multiple providers for reliability, cost optimization, and fallback.

### Architecture

```
┌──────────────────────────────────────────────┐
│            Your Applications                │
├──────────────────────────────────────────────┤
│  Chatbot  │  Content Gen  │  Analysis Tool  │
└─────────────────┬──────────────────────────┘
                  │
                  ▼
        ┌─────────────────────────┐
        │    SmartRouter          │
        │  (Adaptive Strategy)    │
        │  - Cost Optimization    │
        │  - Latency Optimization │
        │  - Quality Optimization │
        └──────┬──────────────────┘
               │
    ┌──────────┼──────────┬──────────┐
    ▼          ▼          ▼          ▼
┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│ Groq   │ │ OpenAI │ │ Claude │ │LMStudio│
└────────┘ └────────┘ └────────┘ └────────┘
  Fast      Quality    Premium    Free
```

### Configuration

```json
{
  "Monado": {
    "Providers": [
      {
        "Name": "Groq",
        "Type": "Groq",
        "Enabled": true,
        "Weight": 0.4,
        "Priority": 1,
        "Configuration": {
          "ApiKey": "${GROQ_API_KEY}",
          "Model": "mixtral-8x7b-32768"
        }
      },
      {
        "Name": "OpenAI",
        "Type": "OpenAI",
        "Enabled": true,
        "Weight": 0.3,
        "Priority": 2,
        "Configuration": {
          "ApiKey": "${OPENAI_API_KEY}",
          "Model": "gpt-3.5-turbo"
        }
      },
      {
        "Name": "Claude",
        "Type": "Anthropic",
        "Enabled": true,
        "Weight": 0.2,
        "Priority": 3,
        "Configuration": {
          "ApiKey": "${ANTHROPIC_API_KEY}",
          "Model": "claude-3-sonnet"
        }
      },
      {
        "Name": "LMStudio",
        "Type": "LMStudio",
        "Enabled": true,
        "Weight": 0.1,
        "Priority": 4,
        "Configuration": {
          "Endpoint": "http://lmstudio:1234"
        }
      }
    ],
    "Routing": {
      "Strategy": "adaptive",
      "CostWeighting": 0.2,
      "LatencyWeighting": 0.3,
      "QualityWeighting": 0.5,
      "FailoverEnabled": true,
      "MaxRetries": 3,
      "BackoffMultiplier": 2.0
    }
  }
}
```

### Startup Code

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMonadoBlade(builder.Configuration);

// Register all providers
builder.Services.AddProviders(builder.Configuration);

// Add adaptive router
builder.Services.AddSingleton<ISmartRouter>(sp =>
{
    var providers = sp.GetRequiredService<IEnumerable<IProviderAdapter>>();
    var context = sp.GetRequiredService<IServiceContext>();
    return new AdaptiveRouter(providers, context);
});

// Add failover controller
builder.Services.AddSingleton<IFailoverController>(sp =>
{
    var providers = sp.GetRequiredService<IEnumerable<IProviderAdapter>>();
    return new FailoverController(providers);
});

var app = builder.Build();

app.UseMonadoBlade();
app.MapInferenceEndpoints();

await app.RunAsync();
```

### Registering Providers

```csharp
// Extension method
public static IServiceCollection AddProviders(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var providerConfigs = configuration
        .GetSection("Monado:Providers")
        .Get<List<ProviderConfig>>();
    
    foreach (var config in providerConfigs.Where(p => p.Enabled))
    {
        switch (config.Type)
        {
            case "OpenAI":
                services.AddSingleton<IProviderAdapter>(sp =>
                    new OpenAIProvider(
                        sp.GetRequiredService<IServiceContext>(),
                        config));
                break;
            
            case "Groq":
                services.AddSingleton<IProviderAdapter>(sp =>
                    new GroqProvider(
                        sp.GetRequiredService<IServiceContext>(),
                        config));
                break;
            
            case "Anthropic":
                services.AddSingleton<IProviderAdapter>(sp =>
                    new AnthropicProvider(
                        sp.GetRequiredService<IServiceContext>(),
                        config));
                break;
            
            case "LMStudio":
                services.AddSingleton<IProviderAdapter>(sp =>
                    new LMStudioProvider(
                        sp.GetRequiredService<IServiceContext>(),
                        config));
                break;
        }
    }
    
    return services;
}
```

---

## Local Hyper-V Deployment

### Scenario: Development & Testing

Run Monado with multiple services on local Hyper-V.

### Prerequisites

```powershell
# Enable Hyper-V
Enable-WindowsOptionalFeature -FeatureName Hyper-V -Online -All

# Create virtual network
New-VMSwitch -Name "MonadoNet" -SwitchType Internal

# Verify
Get-VMSwitch
```

### VM Configuration

Create two VMs:

**VM 1: Monado Hub**
- OS: Windows Server 2022 or Ubuntu 22.04
- RAM: 8GB
- Disk: 100GB
- Network: MonadoNet

**VM 2: LM Studio (Optional)**
- OS: Windows Server 2022 (for GPU support)
- RAM: 16GB
- Disk: 150GB
- GPU: Passed through NVIDIA GPU
- Network: MonadoNet

### Installation on VM1

```powershell
# Install .NET 8
winget install Microsoft.DotNet.SDK.8

# Clone repository
git clone https://github.com/company/monado-blade.git
cd monado-blade

# Set environment variables
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "your-key")
[Environment]::SetEnvironmentVariable("GROQ_API_KEY", "your-key")
[Environment]::SetEnvironmentVariable("LMSTUDIO_URL", "http://10.0.0.2:1234")

# Build
dotnet build

# Create Windows Service
sc create MonadoService `
  binPath= "C:\monado-blade\bin\Release\net8.0\Monado.Server.exe" `
  start= auto

# Start service
Start-Service MonadoService

# Verify
Test-NetConnection -ComputerName localhost -Port 8080
```

### Installation on VM2 (LM Studio)

```powershell
# Download LM Studio
# https://lmstudio.ai

# Extract and run
.\LM Studio\lm-studio.exe

# Configure server on port 1234
# Settings → Server → Port 1234

# Verify from VM1
curl http://10.0.0.2:1234/api/config
```

### Docker Compose Alternative

```yaml
version: '3.8'

services:
  monado:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      OPENAI_API_KEY: ${OPENAI_API_KEY}
      GROQ_API_KEY: ${GROQ_API_KEY}
      LMSTUDIO_URL: http://lmstudio:1234
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      - lmstudio
    networks:
      - monado

  lmstudio:
    image: ghcr.io/lm-sys/lmstudio:latest
    ports:
      - "1234:1234"
    volumes:
      - lmstudio_data:/app/data
    networks:
      - monado

networks:
  monado:
    driver: bridge

volumes:
  lmstudio_data:
```

---

## Cloud Deployments

### Azure App Service

**Architecture:**
```
├─ App Service (Monado)
├─ Key Vault (API Keys)
├─ Application Insights (Monitoring)
└─ Storage Account (Logs)
```

**Deployment:**

```powershell
# Create resource group
az group create --name monado-rg --location eastus

# Create App Service Plan
az appservice plan create `
  --name monado-plan `
  --resource-group monado-rg `
  --sku B2 `
  --is-linux

# Create Web App
az webapp create `
  --resource-group monado-rg `
  --plan monado-plan `
  --name monado-app `
  --runtime "DOTNET|8.0"

# Configure environment
az webapp config appsettings set `
  --resource-group monado-rg `
  --name monado-app `
  --settings `
    OPENAI_API_KEY=@Microsoft.KeyVault(SecretUri=https://...) `
    GROQ_API_KEY=@Microsoft.KeyVault(SecretUri=https://...) `
    ASPNETCORE_ENVIRONMENT=Production

# Deploy from git
az webapp deployment source config-zip `
  --resource-group monado-rg `
  --name monado-app `
  --src build.zip
```

### AWS Lambda

**Architecture:**
```
├─ API Gateway
├─ Lambda (Monado)
├─ Secrets Manager (API Keys)
└─ CloudWatch (Monitoring)
```

**Deployment:**

```bash
# Create deployment package
dotnet publish -c Release -o publish
cd publish
zip -r function.zip .
cd ..

# Upload to Lambda
aws lambda create-function \
  --function-name monado \
  --zip-file fileb://function.zip \
  --handler Monado.Server::Program.LambdaHandler \
  --runtime dotnet8 \
  --role arn:aws:iam::ACCOUNT:role/lambda-role

# Configure environment
aws lambda update-function-configuration \
  --function-name monado \
  --environment Variables={OPENAI_API_KEY=xxx,GROQ_API_KEY=yyy}
```

### Google Cloud Run

**Architecture:**
```
├─ Cloud Run (Monado)
├─ Secret Manager (API Keys)
└─ Cloud Logging
```

**Deployment:**

```bash
# Build container
gcloud builds submit --tag gcr.io/PROJECT/monado

# Deploy
gcloud run deploy monado \
  --image gcr.io/PROJECT/monado \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars OPENAI_API_KEY=@project/OPENAI_API_KEY

# Verify
curl https://monado-XXXXX.run.app/health
```

---

## Production Setup

### Checklist

- [ ] **Security**
  - [ ] All API keys in secrets manager
  - [ ] HTTPS enabled
  - [ ] CORS properly configured
  - [ ] Rate limiting enabled
  - [ ] Input validation in place

- [ ] **Monitoring**
  - [ ] Logging configured
  - [ ] Metrics collection enabled
  - [ ] Alerting rules set up
  - [ ] Health checks working

- [ ] **Performance**
  - [ ] Caching enabled
  - [ ] Connection pooling configured
  - [ ] Timeouts set appropriately
  - [ ] Autoscaling enabled

- [ ] **Reliability**
  - [ ] Failover configured
  - [ ] Backup providers available
  - [ ] Circuit breakers enabled
  - [ ] Error handling comprehensive

### Production appsettings.json

```json
{
  "Monado": {
    "Service": {
      "Name": "MonadoProduction",
      "Environment": "Production",
      "LogLevel": "Warning",
      "CorrelationIdHeader": "X-Correlation-Id"
    },
    "Security": {
      "RequireHttps": true,
      "AllowedOrigins": ["https://yourdomain.com"],
      "RateLimiting": {
        "Enabled": true,
        "RequestsPerMinute": 1000,
        "BurstSize": 50
      },
      "ApiKeyHeader": "X-API-Key",
      "ValidateApiKey": true
    },
    "Providers": [
      {
        "Name": "OpenAI",
        "Enabled": true,
        "Weight": 0.4,
        "Timeout": 30000,
        "MaxRetries": 3
      },
      {
        "Name": "Groq",
        "Enabled": true,
        "Weight": 0.5,
        "Timeout": 20000,
        "MaxRetries": 2
      },
      {
        "Name": "LMStudio",
        "Enabled": true,
        "Weight": 0.1,
        "Timeout": 60000,
        "MaxRetries": 1
      }
    ],
    "Routing": {
      "Strategy": "adaptive",
      "CostWeighting": 0.2,
      "LatencyWeighting": 0.3,
      "QualityWeighting": 0.5,
      "FailoverEnabled": true,
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 5,
        "ResetTimeout": 60000
      }
    },
    "Cache": {
      "Enabled": true,
      "Type": "Redis",
      "ConnectionString": "${REDIS_CONNECTION_STRING}",
      "Duration": 3600,
      "SlidingExpiration": 300
    },
    "Logging": {
      "Console": true,
      "File": {
        "Enabled": true,
        "Path": "/var/log/monado",
        "MaxFileSize": 104857600,
        "MaxRetainedFiles": 30
      },
      "ApplicationInsights": {
        "Enabled": true,
        "InstrumentationKey": "${AI_INSTRUMENTATION_KEY}"
      }
    },
    "Metrics": {
      "Enabled": true,
      "ExportInterval": 60,
      "Prometheus": {
        "Enabled": true,
        "Port": 9090
      }
    }
  },
  "Serilog": {
    "MinimumLevel": "Warning",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/monado/app-.log",
          "rollingInterval": "Day"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "instrumentationKey": "${AI_INSTRUMENTATION_KEY}"
        }
      }
    ]
  }
}
```

---

## Monitoring & Alerting

### Health Checks

```csharp
[HttpGet("health/detailed")]
public async Task<IActionResult> DetailedHealth()
{
    var providers = HttpContext.RequestServices
        .GetRequiredService<IEnumerable<IProviderAdapter>>();
    
    var health = new
    {
        timestamp = DateTime.UtcNow,
        overall_status = "healthy",
        providers = await Task.WhenAll(
            providers.Select(async p => new
            {
                name = p.ProviderName,
                status = (await p.GetStatusAsync()).ToString(),
                latency_ms = p.AverageLatencyMs,
                cost_per_1k = p.CostPer1kTokens,
                capabilities = p.Capabilities
            })
        )
    };
    
    return Ok(health);
}
```

### Metrics Collection

```csharp
public class MetricsCollector
{
    private readonly Counter _requestCounter;
    private readonly Histogram _latencyHistogram;
    private readonly Counter _errorCounter;
    
    public MetricsCollector(MeterProvider meterProvider)
    {
        var meter = meterProvider.GetMeter("Monado");
        
        _requestCounter = meter.CreateCounter<long>("requests_total");
        _latencyHistogram = meter.CreateHistogram<long>("request_latency_ms");
        _errorCounter = meter.CreateCounter<long>("errors_total");
    }
    
    public void RecordRequest(string provider, long latencyMs)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object>("provider", provider));
        _latencyHistogram.Record(latencyMs, new KeyValuePair<string, object>("provider", provider));
    }
    
    public void RecordError(string provider, string errorType)
    {
        _errorCounter.Add(1, new[] {
            new KeyValuePair<string, object>("provider", provider),
            new KeyValuePair<string, object>("error_type", errorType)
        });
    }
}
```

### Alert Rules (Application Insights)

```json
{
  "alerts": [
    {
      "name": "HighErrorRate",
      "condition": "error_rate > 5%",
      "window": "5m",
      "severity": "critical"
    },
    {
      "name": "HighLatency",
      "condition": "p99_latency > 2000",
      "window": "5m",
      "severity": "warning"
    },
    {
      "name": "ProviderDown",
      "condition": "provider_health == 'unhealthy'",
      "window": "2m",
      "severity": "critical"
    },
    {
      "name": "BudgetExceeded",
      "condition": "daily_cost > budget",
      "window": "1h",
      "severity": "warning"
    }
  ]
}
```

---

## Performance Tuning

### Connection Pooling

```csharp
services.AddHttpClient<OpenAIProvider>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Monado/2.4.0");
    })
    .ConfigureHttpMessageHandlerBuilder(builder =>
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 100
        };
        builder.PrimaryHandler = handler;
    });
```

### Response Caching

```csharp
app.UseResponseCaching();

app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(1)
        };
    await next();
});
```

### Request Batching

```csharp
[HttpPost("batch")]
public async Task<IActionResult> BatchInference([FromBody] BatchRequest[] requests)
{
    var tasks = requests
        .Select(async req =>
        {
            var response = await _router.InferenceAsync(
                req.ToInferenceRequest(),
                _routingStrategy);
            return response;
        });
    
    var results = await Task.WhenAll(tasks);
    return Ok(results);
}
```

---

## Troubleshooting

### Issue: "Connection Refused"

```powershell
# Check if service is running
Get-Process Monado.Server

# Check port
netstat -ano | findstr :8080

# Check firewall
netsh advfirewall firewall add rule `
  name="Monado" `
  dir=in `
  action=allow `
  protocol=tcp `
  localport=8080
```

### Issue: "Out of Memory"

```csharp
// Configure memory limits
services.AddMemoryCache(options =>
{
    options.SizeLimit = 536870912;  // 512MB
});

// Use cache size tracking
cache.Set(key, value, new MemoryCacheEntryOptions
{
    Size = 1,
    SizeLimit = 512
});
```

### Issue: "API Key Invalid"

```powershell
# Verify environment variable
$env:OPENAI_API_KEY

# Verify in Key Vault
az keyvault secret show --vault-name monado-kv --name OPENAI-API-KEY

# Test API key directly
curl -H "Authorization: Bearer $env:OPENAI_API_KEY" \
  https://api.openai.com/v1/models
```

---

## Deployment Checklist

Before going live:

- [ ] All API keys secured
- [ ] HTTPS configured
- [ ] Health checks passing
- [ ] Monitoring active
- [ ] Rate limiting set
- [ ] Failover tested
- [ ] Load tested
- [ ] Backup procedure documented
- [ ] Rollback procedure tested
- [ ] On-call rotation established

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
