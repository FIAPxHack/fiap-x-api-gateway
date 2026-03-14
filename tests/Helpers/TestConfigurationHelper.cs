using Microsoft.Extensions.Configuration;

namespace Gateway.Tests.Helpers;

public static class TestConfigurationHelper
{
    public static IConfiguration CreateJwtConfiguration(
        string secret = "test-secret-key-with-at-least-32-chars",
        string issuer = "test-issuer",
        string audience = "test-audience")
    {
        var configData = new Dictionary<string, string>
        {
            { "Jwt:Secret", secret },
            { "Jwt:Issuer", issuer },
            { "Jwt:Audience", audience }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    public static IConfiguration CreateServicesConfiguration(
        string userServiceUrl = "http://localhost:8080",
        string authServiceUrl = "http://auth-service:8080",
        string videoServiceUrl = "http://video-service:8080",
        string notificationServiceUrl = "http://notification-service:8080")
    {
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", userServiceUrl },
            { "Services:AuthService:Url", authServiceUrl },
            { "Services:VideoProcessingService:Url", videoServiceUrl },
            { "Services:NotificationService:Url", notificationServiceUrl }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}