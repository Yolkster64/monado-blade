using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Helios.Connect.Api;
using Helios.Connect.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var deliveries = new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.Ordinal);

app.MapGet("/health", () => Results.Ok(new { service = "helios-connect", status = "healthy", mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run" }));

app.MapPost("/runtime/webhooks/mcp", async (HttpContext context) =>
{
    var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
    if (mode.Equals("live", StringComparison.OrdinalIgnoreCase)) return Results.StatusCode(StatusCodes.Status403Forbidden);
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && !System.Net.IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);

    JsonDocument document;
    try { document = await JsonDocument.ParseAsync(context.Request.Body); }
    catch (JsonException) { return Results.BadRequest(new { error = "invalid JSON-RPC" }); }
    using (document)
    {
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idValue) ? idValue.Clone() : default;
        var method = root.TryGetProperty("method", out var methodValue) ? methodValue.GetString() : null;
        object result = method switch
        {
            "initialize" => new { protocolVersion = "2025-03-26", capabilities = new { tools = new { } }, serverInfo = new { name = "helios-local", version = "0.1.0" } },
            "notifications/initialized" => new { },
            "tools/list" => new { tools = new object[] {
                new { name = "hermes_get_status", description = "Read Helios/Hermes local status.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } },
                new { name = "hermes_list_routes", description = "List configured integration route names without secrets.", inputSchema = new { type = "object", properties = new { }, additionalProperties = false } }
            } },
            "tools/call" => BuildToolResult(root),
            _ => new { error = "unsupported method" }
        };
        return Results.Json(new { jsonrpc = "2.0", id, result });
    }
});

app.MapPost("/webhooks/{provider}", async (string provider, HttpRequest request) =>
{
    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "teams", "sharepoint", "foundry", "copilot" };
    if (!supported.Contains(provider)) return Results.NotFound();

    var maxBytes = int.TryParse(Environment.GetEnvironmentVariable("HELIOS_MAX_WEBHOOK_BYTES"), out var configuredMax) ? configuredMax : 1_048_576;
    if (request.ContentLength is > 0 && request.ContentLength > maxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body)) return Results.BadRequest(new { error = "empty payload" });
    if (Encoding.UTF8.GetByteCount(body) > maxBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    try { using var _ = JsonDocument.Parse(body); }
    catch (JsonException) { return Results.BadRequest(new { error = "invalid JSON" }); }

    var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
    if (mode.Equals("live", StringComparison.OrdinalIgnoreCase) && !WebhookVerifier.Verify(provider, request.Headers, body))
        return Results.Unauthorized();

    var deliveryId = request.Headers["X-GitHub-Delivery"].FirstOrDefault()
        ?? request.Headers["X-Linear-Delivery"].FirstOrDefault()
        ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    if (!deliveries.TryAdd($"{provider}:{deliveryId}", DateTimeOffset.UtcNow)) return Results.Ok(new { duplicate = true, deliveryId });
    var evt = new HeliosEvent(deliveryId, $"{provider}.received", provider, provider,
        DateTimeOffset.UtcNow, Guid.NewGuid().ToString("n"), request.Headers["traceparent"].FirstOrDefault(),
        "internal", new Dictionary<string, object?> { ["rawSize"] = body.Length });
    return Results.Accepted(value: evt);
});

app.Run();

static object BuildToolResult(JsonElement root)
{
    var name = root.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("name", out var nameValue)
        ? nameValue.GetString()
        : null;
    var payload = name switch
    {
        "hermes_get_status" => JsonSerializer.Serialize(new { status = "dry-run", learning = "candidate-only", writes = "pull-request-only" }),
        "hermes_list_routes" => JsonSerializer.Serialize(new[] { "github.workflow.failed", "linear.issue.updated", "hermes.training.candidate", "hermes.training.approved", "sharepoint.document.updated" }),
        _ => JsonSerializer.Serialize(new { error = "unknown tool" })
    };
    return new { content = new[] { new { type = "text", text = payload } }, isError = name is not ("hermes_get_status" or "hermes_list_routes") };
}

public partial class Program { }
