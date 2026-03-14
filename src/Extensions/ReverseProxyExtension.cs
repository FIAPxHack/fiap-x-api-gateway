using ApiGateway.Configurations;
using ApiGateway.Constants;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions;

public static class ReverseProxyExtension
{
    private const string UserServiceCluster = "userServiceCluster";
    private const string AuthCluster = "authCluster";
    private const string VideoProcessingCluster = "videoProcessingCluster";
    private const string NotificationCluster = "notificationCluster";

    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        var servicesConfig = configuration.GetSection("Services").Get<ServicesConfiguration>()
            ?? throw new InvalidOperationException("Services configuration is missing");

        servicesConfig.UserService.Validate("UserService");
        servicesConfig.AuthService.Validate("AuthService");
        servicesConfig.VideoProcessingService.Validate("VideoProcessingService");
        servicesConfig.NotificationService.Validate("NotificationService");

        var routes = BuildRoutes();
        var clusters = BuildClusters(servicesConfig);

        services.AddReverseProxy()
            .LoadFromMemory(routes, clusters);

        return services;
    }

    private static RouteConfig CreateRoute(
        string routeId, string clusterId, string path, string[] methods,
        string? authorizationPolicy = null,
        IReadOnlyList<IReadOnlyDictionary<string, string>>? transforms = null)
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = path, Methods = methods },
            AuthorizationPolicy = authorizationPolicy,
            Transforms = transforms
        };
    }

    private static ClusterConfig CreateCluster(string clusterId, string destinationName, string address)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                { destinationName, new DestinationConfig { Address = address } }
            }
        };
    }

    private static RouteConfig[] BuildRoutes()
    {
        var authenticated = Policies.AUTHENTICATED_USER;

        return new[]
        {
            // AUTH - Login (público)
            CreateRoute("auth-login", AuthCluster, "/api/auth/login", new[] { "POST", "OPTIONS" },
                transforms: new[] { new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" } }),

            // USUÁRIOS
            CreateRoute("users-create", UserServiceCluster, "/api/users", new[] { "POST" }),
            CreateRoute("users-list", UserServiceCluster, "/api/users", new[] { "GET" }, authenticated),
            CreateRoute("users-get-by-id", UserServiceCluster, "/api/users/{id}", new[] { "GET" }, authenticated),
            CreateRoute("users-update", UserServiceCluster, "/api/users", new[] { "PUT" }, authenticated),
            CreateRoute("users-delete", UserServiceCluster, "/api/users/{id}", new[] { "DELETE" }, authenticated),

            // VIDEO PROCESSING
            CreateRoute("video-upload", VideoProcessingCluster, "/api/videos/upload", new[] { "POST" }, authenticated),
            CreateRoute("video-status", VideoProcessingCluster, "/api/videos/{id}/status", new[] { "GET" }, authenticated),
            CreateRoute("video-update-status", VideoProcessingCluster, "/api/videos/{id}/status", new[] { "PUT" }, authenticated),
            CreateRoute("video-download", VideoProcessingCluster, "/api/videos/{id}/download", new[] { "GET" }, authenticated),
            CreateRoute("video-list-user", VideoProcessingCluster, "/api/videos/user/{userId}", new[] { "GET" }, authenticated),

            // NOTIFICATIONS
            CreateRoute("notification-send", NotificationCluster, "/api/notifications/send", new[] { "POST" }, authenticated),
            CreateRoute("notification-user-list", NotificationCluster, "/api/notifications/user/{userId}", new[] { "GET" }, authenticated)
        };
    }

    private static ClusterConfig[] BuildClusters(ServicesConfiguration config)
    {
        return new[]
        {
            CreateCluster(AuthCluster, "authDestination", config.AuthService.Url),
            CreateCluster(UserServiceCluster, "userServiceDestination", config.UserService.Url),
            CreateCluster(VideoProcessingCluster, "videoProcessingDestination", config.VideoProcessingService.Url),
            CreateCluster(NotificationCluster, "notificationDestination", config.NotificationService.Url)
        };
    }
}