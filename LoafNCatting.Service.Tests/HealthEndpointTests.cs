using System.Net;
using LoafNCatting.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LoafNCatting.Service.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        await using var factory = new WebApplicationFactory<AuthController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LoafNCattingMobile"] =
                            "Server=localhost;Database=LoafNCattingMobile;User Id=sa;Password=Test_password_123;TrustServerCertificate=True;Encrypt=False",
                        ["PayOS:ClientId"] = "test-client-id",
                        ["PayOS:ApiKey"] = "test-api-key",
                        ["PayOS:ChecksumKey"] = "test-checksum-key"
                    });
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
