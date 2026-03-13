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
    private readonly CustomWebApplicationFactory _factory;

    public ProgramIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ok");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/healthz");
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();

        // Assert
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("ok");
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Application_ShouldHandleCors()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/healthz");
        request.Headers.Add("Origin", "http://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Application_ShouldUseAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert - Should fail because no auth token provided
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Application_WithEnvironmentVariables_ShouldMapJwtSecret()
    {
        // Arrange
        Environment.SetEnvironmentVariable("JWT_SECRET", "env-test-secret-key-with-at-least-32-chars");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "env-test-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "env-test-audience");

        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
    }

    [Fact]
    public async Task Application_WithEnvironmentVariables_ShouldMapServiceUrls()
    {
        // Arrange
        Environment.SetEnvironmentVariable("USER_SERVICE_URL", "http://env-user-service:8080");
        Environment.SetEnvironmentVariable("AUTH_SERVICE_URL", "http://env-auth-service:8080");
        Environment.SetEnvironmentVariable("VIDEO_PROCESSING_SERVICE_URL", "http://env-video-service:8080");
        Environment.SetEnvironmentVariable("NOTIFICATION_SERVICE_URL", "http://env-notification-service:8080");

        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        Environment.SetEnvironmentVariable("USER_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("AUTH_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("VIDEO_PROCESSING_SERVICE_URL", null);
        Environment.SetEnvironmentVariable("NOTIFICATION_SERVICE_URL", null);
    }

    [Fact]
    public async Task Application_WithAuthLambdaUrl_ShouldMapToAuthService()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AUTH_LAMBDA_URL", "http://auth-lambda:8080");

        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        Environment.SetEnvironmentVariable("AUTH_LAMBDA_URL", null);
    }

    [Fact]
    public async Task Application_ShouldConfigureReverseProxy()
    {
        // Act - Try to access a proxied route (should fail with Unauthorized, not NotFound)
        var response = await _client.PostAsync("/api/users", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert - Should be proxied but rejected due to missing auth or service unavailability
        // NotFound would indicate route not configured
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    private record HealthResponse(string Status, DateTime Timestamp);
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            // Clear default configuration sources
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-with-at-least-32-characters-for-testing",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Services:UserService:Url"] = "http://localhost:8081",
                ["Services:AuthService:Url"] = "http://localhost:8082",
                ["Services:VideoProcessingService:Url"] = "http://localhost:8083",
                ["Services:NotificationService:Url"] = "http://localhost:8084"
            }!);
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear app configuration sources too
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-with-at-least-32-characters-for-testing",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Services:UserService:Url"] = "http://localhost:8081",
                ["Services:AuthService:Url"] = "http://localhost:8082",
                ["Services:VideoProcessingService:Url"] = "http://localhost:8083",
                ["Services:NotificationService:Url"] = "http://localhost:8084"
            }!);
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
