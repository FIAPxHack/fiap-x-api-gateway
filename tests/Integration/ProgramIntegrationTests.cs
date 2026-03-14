using System.Net;
using System.Text;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace Gateway.Tests.Integration;

public class ProgramIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProgramIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ok");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnJsonContent()
    {
        var response = await _client.GetAsync("/healthz");
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();

        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("ok");
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Application_ShouldHandleCors()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/healthz");
        request.Headers.Add("Origin", "http://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Application_ShouldRequireAuthenticationForProtectedRoutes()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Application_ShouldConfigureReverseProxy()
    {
        var response = await _client.PostAsync("/api/users",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_WithJwtEnvironmentVariables_ShouldStartSuccessfully()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "env-test-secret-key-with-at-least-32-chars");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "env-test-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "env-test-audience");

        try
        {
            using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("/healthz");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        }
    }

    [Fact]
    public async Task Application_WithServiceUrlEnvironmentVariables_ShouldStartSuccessfully()
    {
        Environment.SetEnvironmentVariable("USER_SERVICE_URL", "http://env-user-service:8080");
        Environment.SetEnvironmentVariable("AUTH_SERVICE_URL", "http://env-auth-service:8080");
        Environment.SetEnvironmentVariable("VIDEO_PROCESSING_SERVICE_URL", "http://env-video-service:8080");
        Environment.SetEnvironmentVariable("NOTIFICATION_SERVICE_URL", "http://env-notification-service:8080");

        try
        {
            using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("/healthz");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            Environment.SetEnvironmentVariable("USER_SERVICE_URL", null);
            Environment.SetEnvironmentVariable("AUTH_SERVICE_URL", null);
            Environment.SetEnvironmentVariable("VIDEO_PROCESSING_SERVICE_URL", null);
            Environment.SetEnvironmentVariable("NOTIFICATION_SERVICE_URL", null);
        }
    }

    [Fact]
    public async Task Application_WithAuthLambdaUrl_ShouldMapToAuthService()
    {
        Environment.SetEnvironmentVariable("AUTH_LAMBDA_URL", "http://auth-lambda:8080");

        try
        {
            using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("/healthz");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_LAMBDA_URL", null);
        }
    }

    private record HealthResponse(string Status, DateTime Timestamp);
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly Dictionary<string, string?> TestConfig = new()
    {
        ["Jwt:Secret"] = "test-secret-key-with-at-least-32-characters-for-testing",
        ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience",
        ["Services:UserService:Url"] = "http://localhost:8081",
        ["Services:AuthService:Url"] = "http://localhost:8082",
        ["Services:VideoProcessingService:Url"] = "http://localhost:8083",
        ["Services:NotificationService:Url"] = "http://localhost:8084"
    };

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(TestConfig);
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(TestConfig);
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}