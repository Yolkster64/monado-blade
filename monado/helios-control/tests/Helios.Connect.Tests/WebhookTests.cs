using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
