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

    private static readonly string[] HttpGet = new[] { "GET" };
    private static readonly string[] HttpPost = new[] { "POST" };
    private static readonly string[] HttpPut = new[] { "PUT" };
    private static readonly string[] HttpDelete = new[] { "DELETE" };
    private static readonly string[] HttpPostOptions = new[] { "POST", "OPTIONS" };
    private static readonly IReadOnlyList<IReadOnlyDictionary<string, string>> PathRemovePrefixApi = new[]
    {
        new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" }
    };

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
            CreateRoute("auth-login", AuthCluster, "/api/auth/login", HttpPostOptions,
                transforms: PathRemovePrefixApi),

            // USUÁRIOS
            CreateRoute("users-create", UserServiceCluster, "/api/users", HttpPost),
            CreateRoute("users-list", UserServiceCluster, "/api/users", HttpGet, authenticated),
            CreateRoute("users-get-by-id", UserServiceCluster, "/api/users/{id}", HttpGet, authenticated),
            CreateRoute("users-update", UserServiceCluster, "/api/users", HttpPut, authenticated),
            CreateRoute("users-delete", UserServiceCluster, "/api/users/{id}", HttpDelete, authenticated),

            // VIDEO PROCESSING
            CreateRoute("video-upload", VideoProcessingCluster, "/api/videos/upload", HttpPost, authenticated),
            CreateRoute("video-status", VideoProcessingCluster, "/api/videos/{id}/status", HttpGet, authenticated),
            CreateRoute("video-update-status", VideoProcessingCluster, "/api/videos/{id}/status", HttpPut, authenticated),
            CreateRoute("video-download", VideoProcessingCluster, "/api/videos/{id}/download", HttpGet, authenticated),
            CreateRoute("video-list-user", VideoProcessingCluster, "/api/videos/user/{userId}", HttpGet, authenticated),

            // NOTIFICATIONS
            CreateRoute("notification-send", NotificationCluster, "/api/notifications/send", HttpPost, authenticated),
            CreateRoute("notification-user-list", NotificationCluster, "/api/notifications/user/{userId}", HttpGet, authenticated)
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