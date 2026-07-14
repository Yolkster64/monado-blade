using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Helios.Connect.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { service = "helios-connect", status = "healthy", mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run" }));

app.MapPost("/webhooks/{provider}", async (string provider, HttpRequest request) =>
{
    var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "github", "linear", "slack", "teams", "sharepoint", "foundry", "copilot" };
    if (!supported.Contains(provider)) return Results.NotFound();

    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body)) return Results.BadRequest(new { error = "empty payload" });

    // Provider-specific signature validators plug in here. Live mode fails closed.
    var mode = Environment.GetEnvironmentVariable("HELIOS_EXECUTION_MODE") ?? "dry-run";
    if (mode.Equals("live", StringComparison.OrdinalIgnoreCase) && !request.Headers.ContainsKey("X-Helios-Signature-Verified"))
        return Results.Unauthorized();

    var deliveryId = request.Headers["X-GitHub-Delivery"].FirstOrDefault()
        ?? request.Headers["X-Linear-Delivery"].FirstOrDefault()
        ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    var evt = new HeliosEvent(deliveryId, $"{provider}.received", provider, provider,
        DateTimeOffset.UtcNow, Guid.NewGuid().ToString("n"), request.Headers["traceparent"].FirstOrDefault(),
        "internal", new Dictionary<string, object?> { ["rawSize"] = body.Length });
    return Results.Accepted(value: evt);
});

app.Run();

public partial class Program { }
