using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Helios.Connect.Tests;

public sealed class WebhookTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public WebhookTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Unknown_provider_is_not_found() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync("/webhooks/nope", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode);

    [Fact]
    public async Task Empty_payload_is_rejected() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/webhooks/github", new StringContent(""))).StatusCode);

    [Fact]
    public async Task Invalid_json_is_rejected() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/webhooks/github", new StringContent("not-json", Encoding.UTF8, "application/json"))).StatusCode);

    [Fact]
    public async Task Local_mcp_lists_only_read_tools()
    {
        var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/runtime/webhooks/mcp", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hermes_get_status", body);
        Assert.DoesNotContain("run_sandbox", body);
    }
}
