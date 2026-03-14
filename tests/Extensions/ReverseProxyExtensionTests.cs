using ApiGateway.Constants;
using ApiGateway.Extensions;
using FluentAssertions;
using Gateway.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Tests.Extensions;

public class ReverseProxyExtensionTests
{
    private static IProxyConfig BuildProxyConfig(IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        configuration ??= TestConfigurationHelper.CreateServicesConfiguration();
        services.AddGatewayReverseProxy(configuration);
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IProxyConfigProvider>().GetConfig();
    }

    [Fact]
    public void AddGatewayReverseProxy_WithValidConfiguration_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddGatewayReverseProxy(TestConfigurationHelper.CreateServicesConfiguration());

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IProxyConfigProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddGatewayReverseProxy_WithMissingConfiguration_ShouldThrowException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddGatewayReverseProxy(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Services configuration is missing");
    }

    [Theory]
    [InlineData("invalid-url", "http://auth:8080", "http://video:8080", "http://notif:8080", "UserService URL é inválida: invalid-url")]
    [InlineData("http://localhost:8080", "not-a-url", "http://video:8080", "http://notif:8080", "AuthService URL é inválida: not-a-url")]
    [InlineData("http://localhost:8080", "http://auth:8080", "invalid-url", "http://notif:8080", "VideoProcessingService URL é inválida: invalid-url")]
    [InlineData("http://localhost:8080", "http://auth:8080", "http://video:8080", "not-valid", "NotificationService URL é inválida: not-valid")]
    public void AddGatewayReverseProxy_WithInvalidServiceUrl_ShouldThrowException(
        string userUrl, string authUrl, string videoUrl, string notifUrl, string expectedMessage)
    {
        var services = new ServiceCollection();
        var configuration = TestConfigurationHelper.CreateServicesConfiguration(userUrl, authUrl, videoUrl, notifUrl);

        var act = () => services.AddGatewayReverseProxy(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("", "http://auth:8080", "http://video:8080", "http://notif:8080", "UserService URL é obrigatória")]
    [InlineData("http://localhost:8080", "", "http://video:8080", "http://notif:8080", "AuthService URL é obrigatória")]
    [InlineData("http://localhost:8080", "http://auth:8080", "", "http://notif:8080", "VideoProcessingService URL é obrigatória")]
    [InlineData("http://localhost:8080", "http://auth:8080", "http://video:8080", "", "NotificationService URL é obrigatória")]
    public void AddGatewayReverseProxy_WithEmptyServiceUrl_ShouldThrowException(
        string userUrl, string authUrl, string videoUrl, string notifUrl, string expectedMessage)
    {
        var services = new ServiceCollection();
        var configuration = TestConfigurationHelper.CreateServicesConfiguration(userUrl, authUrl, videoUrl, notifUrl);

        var act = () => services.AddGatewayReverseProxy(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureAllExpectedRoutes()
    {
        var proxyConfig = BuildProxyConfig();

        var expectedRouteIds = new[]
        {
            "auth-login",
            "users-create", "users-list", "users-get-by-id", "users-update", "users-delete",
            "video-upload", "video-status", "video-update-status", "video-download", "video-list-user",
            "notification-send", "notification-user-list"
        };

        proxyConfig.Routes.Should().HaveCount(expectedRouteIds.Length);
        foreach (var routeId in expectedRouteIds)
        {
            proxyConfig.Routes.Should().Contain(r => r.RouteId == routeId,
                $"because {routeId} should be configured");
        }
    }

    [Fact]
    public void AddGatewayReverseProxy_ShouldConfigureAllClusters()
    {
        var proxyConfig = BuildProxyConfig();

        proxyConfig.Clusters.Should().HaveCount(4);

        var authCluster = proxyConfig.Clusters.First(c => c.ClusterId == "authCluster");
        authCluster.Destinations.Should().ContainKey("authDestination");
        authCluster.Destinations!["authDestination"]!.Address.Should().Be("http://auth-service:8080");

        var userCluster = proxyConfig.Clusters.First(c => c.ClusterId == "userServiceCluster");
        userCluster.Destinations.Should().ContainKey("userServiceDestination");
        userCluster.Destinations!["userServiceDestination"]!.Address.Should().Be("http://localhost:8080");

        var videoCluster = proxyConfig.Clusters.First(c => c.ClusterId == "videoProcessingCluster");
        videoCluster.Destinations.Should().ContainKey("videoProcessingDestination");
        videoCluster.Destinations!["videoProcessingDestination"]!.Address.Should().Be("http://video-service:8080");

        var notifCluster = proxyConfig.Clusters.First(c => c.ClusterId == "notificationCluster");
        notifCluster.Destinations.Should().ContainKey("notificationDestination");
        notifCluster.Destinations!["notificationDestination"]!.Address.Should().Be("http://notification-service:8080");
    }

    [Fact]
    public void AddGatewayReverseProxy_AuthLoginRoute_ShouldBePublicWithTransform()
    {
        var proxyConfig = BuildProxyConfig();

        var route = proxyConfig.Routes.First(r => r.RouteId == "auth-login");
        route.ClusterId.Should().Be("authCluster");
        route.Match.Path.Should().Be("/api/auth/login");
        route.Match.Methods.Should().Contain("POST");
        route.Match.Methods.Should().Contain("OPTIONS");
        route.AuthorizationPolicy.Should().BeNull();
    }

    [Fact]
    public void AddGatewayReverseProxy_UsersCreateRoute_ShouldBePublic()
    {
        var proxyConfig = BuildProxyConfig();

        var route = proxyConfig.Routes.First(r => r.RouteId == "users-create");
        route.ClusterId.Should().Be("userServiceCluster");
        route.Match.Path.Should().Be("/api/users");
        route.Match.Methods.Should().Contain("POST");
        route.AuthorizationPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData("users-list", "userServiceCluster")]
    [InlineData("users-get-by-id", "userServiceCluster")]
    [InlineData("users-update", "userServiceCluster")]
    [InlineData("users-delete", "userServiceCluster")]
    [InlineData("video-upload", "videoProcessingCluster")]
    [InlineData("video-status", "videoProcessingCluster")]
    [InlineData("video-update-status", "videoProcessingCluster")]
    [InlineData("video-download", "videoProcessingCluster")]
    [InlineData("video-list-user", "videoProcessingCluster")]
    [InlineData("notification-send", "notificationCluster")]
    [InlineData("notification-user-list", "notificationCluster")]
    public void AddGatewayReverseProxy_ProtectedRoutes_ShouldRequireAuthentication(
        string routeId, string expectedClusterId)
    {
        var proxyConfig = BuildProxyConfig();

        var route = proxyConfig.Routes.First(r => r.RouteId == routeId);
        route.ClusterId.Should().Be(expectedClusterId);
        route.AuthorizationPolicy.Should().Be(Policies.AUTHENTICATED_USER);
    }
}