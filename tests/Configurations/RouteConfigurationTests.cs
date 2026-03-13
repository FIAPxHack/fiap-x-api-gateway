using ApiGateway.Configurations;
using FluentAssertions;
using Xunit;

namespace Gateway.Tests.Configurations;

public class ServiceEndpointTests
{
    [Fact]
    public void Validate_WithValidUrl_ShouldNotThrow()
    {
        // Arrange
        var endpoint = new ServiceEndpoint { Url = "http://localhost:8080" };

        // Act
        var act = () => endpoint.Validate("TestService");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyUrl_ShouldThrowInvalidOperationException(string url)
    {
        // Arrange
        var endpoint = new ServiceEndpoint { Url = url };

        // Act
        var act = () => endpoint.Validate("TestService");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("TestService URL é obrigatória");
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp:/incomplete")]
    [InlineData("://missing-scheme")]
    public void Validate_WithInvalidUrl_ShouldThrowInvalidOperationException(string invalidUrl)
    {
        // Arrange
        var endpoint = new ServiceEndpoint { Url = invalidUrl };

        // Act
        var act = () => endpoint.Validate("TestService");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"TestService URL é inválida: {invalidUrl}");
    }

    [Theory]
    [InlineData("http://localhost:8080")]
    [InlineData("https://api.example.com")]
    [InlineData("http://192.168.1.1:3000")]
    [InlineData("https://subdomain.example.com:8443/path")]
    public void Validate_WithVariousValidUrls_ShouldNotThrow(string validUrl)
    {
        // Arrange
        var endpoint = new ServiceEndpoint { Url = validUrl };

        // Act
        var act = () => endpoint.Validate("TestService");

        // Assert
        act.Should().NotThrow();
    }
}

public class ServicesConfigurationTests
{
    [Fact]
    public void Properties_ShouldBeInitializedByDefault()
    {
        // Arrange & Act
        var config = new ServicesConfiguration();

        // Assert
        config.UserService.Should().NotBeNull();
        config.AuthService.Should().NotBeNull();
        config.VideoProcessingService.Should().NotBeNull();
        config.NotificationService.Should().NotBeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var userService = new ServiceEndpoint { Url = "http://user-service:8080" };
        var authService = new ServiceEndpoint { Url = "http://auth-service:8080" };
        var videoService = new ServiceEndpoint { Url = "http://video-service:8080" };
        var notificationService = new ServiceEndpoint { Url = "http://notification-service:8080" };

        // Act
        var config = new ServicesConfiguration
        {
            UserService = userService,
            AuthService = authService,
            VideoProcessingService = videoService,
            NotificationService = notificationService
        };

        // Assert
        config.UserService.Should().BeSameAs(userService);
        config.AuthService.Should().BeSameAs(authService);
        config.VideoProcessingService.Should().BeSameAs(videoService);
        config.NotificationService.Should().BeSameAs(notificationService);
    }
}