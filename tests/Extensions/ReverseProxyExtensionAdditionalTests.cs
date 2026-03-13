using ApiGateway.Constants;
using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Tests.Extensions;

public class ReverseProxyExtensionAdditionalTests
{
    [Fact]
    public void AddGatewayReverseProxy_WithInvalidVideoProcessingServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthService:Url", "http://auth-service:8080" },
            { "Services:VideoProcessingService:Url", "invalid-url" },
            { "Services:NotificationService:Url", "http://notification-service:8080" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("VideoProcessingService URL é inválida: invalid-url");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithInvalidNotificationServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthService:Url", "http://auth-service:8080" },
            { "Services:VideoProcessingService:Url", "http://video-service:8080" },
            { "Services:NotificationService:Url", "not-valid" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("NotificationService URL é inválida: not-valid");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithEmptyVideoProcessingServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthService:Url", "http://auth-service:8080" },
            { "Services:VideoProcessingService:Url", "" },
            { "Services:NotificationService:Url", "http://notification-service:8080" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("VideoProcessingService URL é obrigatória");
    }

    [Fact]
    public void AddGatewayReverseProxy_WithEmptyNotificationServiceUrl_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthService:Url", "http://auth-service:8080" },
            { "Services:VideoProcessingService:Url", "http://video-service:8080" },
            { "Services:NotificationService:Url", "" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var act = () => services.AddGatewayReverseProxy(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("NotificationService URL é obrigatória");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureVideoProcessingCluster()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var videoCluster = proxyConfig.Clusters.FirstOrDefault(c => c.ClusterId == "videoProcessingCluster");
        videoCluster.Should().NotBeNull();
        videoCluster!.Destinations.Should().ContainKey("videoProcessingDestination");
        videoCluster.Destinations["videoProcessingDestination"].Address.Should().Be("http://video-service:8080");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureNotificationCluster()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var notificationCluster = proxyConfig.Clusters.FirstOrDefault(c => c.ClusterId == "notificationCluster");
        notificationCluster.Should().NotBeNull();
        notificationCluster!.Destinations.Should().ContainKey("notificationDestination");
        notificationCluster.Destinations["notificationDestination"].Address.Should().Be("http://notification-service:8080");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureAuthLoginRoute()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var authLoginRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "auth-login");
        authLoginRoute.Should().NotBeNull();
        authLoginRoute!.ClusterId.Should().Be("authCluster");
        authLoginRoute.Match.Path.Should().Be("/api/auth/login");
        authLoginRoute.Match.Methods.Should().Contain("POST");
        authLoginRoute.Match.Methods.Should().Contain("OPTIONS");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureUsersCreateRoute()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var usersCreateRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "users-create");
        usersCreateRoute.Should().NotBeNull();
        usersCreateRoute!.ClusterId.Should().Be("userServiceCluster");
        usersCreateRoute.Match.Path.Should().Be("/api/users");
        usersCreateRoute.Match.Methods.Should().Contain("POST");
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureProtectedUsersRoutes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var usersListRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "users-list");
        usersListRoute.Should().NotBeNull();
        usersListRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var usersGetByIdRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "users-get-by-id");
        usersGetByIdRoute.Should().NotBeNull();
        usersGetByIdRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var usersUpdateRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "users-update");
        usersUpdateRoute.Should().NotBeNull();
        usersUpdateRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var usersDeleteRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "users-delete");
        usersDeleteRoute.Should().NotBeNull();
        usersDeleteRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureVideoRoutes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var videoUploadRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "video-upload");
        videoUploadRoute.Should().NotBeNull();
        videoUploadRoute!.ClusterId.Should().Be("videoProcessingCluster");
        videoUploadRoute.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var videoStatusRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "video-status");
        videoStatusRoute.Should().NotBeNull();
        videoStatusRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var videoUpdateStatusRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "video-update-status");
        videoUpdateStatusRoute.Should().NotBeNull();
        videoUpdateStatusRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var videoDownloadRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "video-download");
        videoDownloadRoute.Should().NotBeNull();
        videoDownloadRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var videoListUserRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "video-list-user");
        videoListUserRoute.Should().NotBeNull();
        videoListUserRoute!.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureNotificationRoutes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var notificationSendRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "notification-send");
        notificationSendRoute.Should().NotBeNull();
        notificationSendRoute!.ClusterId.Should().Be("notificationCluster");
        notificationSendRoute.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);

        var notificationUserListRoute = proxyConfig.Routes.FirstOrDefault(r => r.RouteId == "notification-user-list");
        notificationUserListRoute.Should().NotBeNull();
        notificationUserListRoute!.ClusterId.Should().Be("notificationCluster");
        notificationUserListRoute.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldHaveAllRequiredRoutes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddGatewayReverseProxy(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var proxyConfigProvider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var proxyConfig = proxyConfigProvider.GetConfig();

        var expectedRouteIds = new[]
        {
            "auth-login",
            "users-create", "users-list", "users-get-by-id", "users-update", "users-delete",
            "video-upload", "video-status", "video-update-status", "video-download", "video-list-user",
            "notification-send", "notification-user-list"
        };

        foreach (var routeId in expectedRouteIds)
        {
            proxyConfig.Routes.Should().Contain(r => r.RouteId == routeId, 
                $"because {routeId} should be configured");
        }
    }

    private static IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            { "Services:UserService:Url", "http://localhost:8080" },
            { "Services:AuthService:Url", "http://auth-service:8080" },
            { "Services:VideoProcessingService:Url", "http://video-service:8080" },
            { "Services:NotificationService:Url", "http://notification-service:8080" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}
