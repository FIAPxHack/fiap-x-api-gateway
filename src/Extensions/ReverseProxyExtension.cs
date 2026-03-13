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

    private static RouteConfig[] BuildRoutes()
    {
        return new[]
        {
            // AUTH - Login (público)
            new RouteConfig
            {
                RouteId = "auth-login",
                ClusterId = AuthCluster,
                Match = new RouteMatch { Path = "/api/auth/login", Methods = new[] { "POST", "OPTIONS" } },
                Transforms = new[]
                {
                    new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" }
                }
            },

            // USUÁRIOS - Create (público - para registro)
            new RouteConfig
            {
                RouteId = "users-create",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users", Methods = new[] { "POST" } }
            },

            // USUÁRIOS - GetAll (requer autenticação)
            new RouteConfig
            {
                RouteId = "users-list",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // USUÁRIOS - GetById (requer autenticação)
            new RouteConfig
            {
                RouteId = "users-get-by-id",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // USUÁRIOS - Update (requer autenticação) - ID vem no body
            new RouteConfig
            {
                RouteId = "users-update",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // USUÁRIOS - Delete (requer autenticação)
            new RouteConfig
            {
                RouteId = "users-delete",
                ClusterId = UserServiceCluster,
                Match = new RouteMatch { Path = "/api/users/{id}", Methods = new[] { "DELETE" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // VIDEO PROCESSING - Upload (requer autenticação)
            new RouteConfig
            {
                RouteId = "video-upload",
                ClusterId = VideoProcessingCluster,
                Match = new RouteMatch { Path = "/api/videos/upload", Methods = new[] { "POST" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // VIDEO PROCESSING - Get Status (requer autenticação)
            new RouteConfig
            {
                RouteId = "video-status",
                ClusterId = VideoProcessingCluster,
                Match = new RouteMatch { Path = "/api/videos/{id}/status", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // VIDEO PROCESSING - Update Status (callback do processor)
            new RouteConfig
            {
                RouteId = "video-update-status",
                ClusterId = VideoProcessingCluster,
                Match = new RouteMatch { Path = "/api/videos/{id}/status", Methods = new[] { "PUT" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // VIDEO PROCESSING - Download ZIP (requer autenticação)
            new RouteConfig
            {
                RouteId = "video-download",
                ClusterId = VideoProcessingCluster,
                Match = new RouteMatch { Path = "/api/videos/{id}/download", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // VIDEO PROCESSING - List User Videos (requer autenticação)
            new RouteConfig
            {
                RouteId = "video-list-user",
                ClusterId = VideoProcessingCluster,
                Match = new RouteMatch { Path = "/api/videos/user/{userId}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // NOTIFICATION - Send Notification (interno/autenticado)
            new RouteConfig
            {
                RouteId = "notification-send",
                ClusterId = NotificationCluster,
                Match = new RouteMatch { Path = "/api/notifications/send", Methods = new[] { "POST" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            },

            // NOTIFICATION - Get User Notifications (requer autenticação)
            new RouteConfig
            {
                RouteId = "notification-user-list",
                ClusterId = NotificationCluster,
                Match = new RouteMatch { Path = "/api/notifications/user/{userId}", Methods = new[] { "GET" } },
                AuthorizationPolicy = Policies.AUTHENTICATED_USER
            }
        };
    }

    private static ClusterConfig[] BuildClusters(ServicesConfiguration config)
    {
        return new[]
        {
            new ClusterConfig
            {
                ClusterId = AuthCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "authDestination", new DestinationConfig { Address = config.AuthService.Url } }
                }
            },
            new ClusterConfig
            {
                ClusterId = UserServiceCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "userServiceDestination", new DestinationConfig { Address = config.UserService.Url } }
                }
            },
            new ClusterConfig
            {
                ClusterId = VideoProcessingCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "videoProcessingDestination", new DestinationConfig { Address = config.VideoProcessingService.Url } }
                }
            },
            new ClusterConfig
            {
                ClusterId = NotificationCluster,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "notificationDestination", new DestinationConfig { Address = config.NotificationService.Url } }
                }
            }
        };
    }
}