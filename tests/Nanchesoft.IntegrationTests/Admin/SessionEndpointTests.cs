using System.Net;
using Nanchesoft.IntegrationTests.Infrastructure;

namespace Nanchesoft.IntegrationTests.Admin;

[Collection("NanchesoftApi")]
public class SessionEndpointTests
{
    private readonly HttpClient _client;

    public SessionEndpointTests(NanchesoftWebFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Is-Platform-Owner", "true");
    }

    [Fact]
    public async Task ListSessions_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/administration/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
