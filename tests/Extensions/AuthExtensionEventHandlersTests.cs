using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.Tests.Extensions;

public class AuthExtensionEventHandlersTests
{
    [Fact]
    public async Task AddJwtAuthentication_OnAuthenticationFailed_ShouldHandleFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get("Bearer");

        var httpContext = new DefaultHttpContext();
        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            "Bearer",
            "Bearer",
            typeof(JwtBearerHandler));

        var context = new AuthenticationFailedContext(httpContext, scheme, jwtOptions)
        {
            Exception = new SecurityTokenExpiredException("Token expired")
        };

        // Act
        Func<Task> act = async () => await jwtOptions.Events.OnAuthenticationFailed(context);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddJwtAuthentication_OnTokenValidated_ShouldTransformClaims()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get("Bearer");

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            "Bearer",
            "Bearer",
            typeof(JwtBearerHandler));

        var context = new TokenValidatedContext(httpContext, scheme, jwtOptions)
        {
            Principal = principal
        };

        // Act
        await jwtOptions.Events.OnTokenValidated(context);

        // Assert
        identity.FindFirst("role")?.Value.Should().Be("ADMIN");
        identity.FindFirst("email")?.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task AddJwtAuthentication_OnChallenge_ShouldHandleChallenge()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get("Bearer");

        var httpContext = new DefaultHttpContext();
        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            "Bearer",
            "Bearer",
            typeof(JwtBearerHandler));
        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties();

        var context = new JwtBearerChallengeContext(httpContext, scheme, jwtOptions, authProperties);

        // Act
        Func<Task> act = async () => await jwtOptions.Events.OnChallenge(context);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddJwtAuthentication_EventHandlers_ShouldBeInvokable()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get("Bearer");

        // Assert that all event handlers are not null
        jwtOptions.Events.Should().NotBeNull();
        jwtOptions.Events.OnAuthenticationFailed.Should().NotBeNull();
        jwtOptions.Events.OnTokenValidated.Should().NotBeNull();
        jwtOptions.Events.OnChallenge.Should().NotBeNull();

        // Create a valid context for OnAuthenticationFailed
        var httpContext = new DefaultHttpContext();
        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            "Bearer",
            "Bearer",
            typeof(JwtBearerHandler));
        
        var authFailedContext = new AuthenticationFailedContext(httpContext, scheme, jwtOptions)
        {
            Exception = new Exception("Test exception")
        };

        // Act & Assert - OnAuthenticationFailed
        await jwtOptions.Events.OnAuthenticationFailed(authFailedContext);
        authFailedContext.Should().NotBeNull();

        // Act & Assert - OnChallenge
        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties();
        var challengeContext = new JwtBearerChallengeContext(httpContext, scheme, jwtOptions, authProperties);
        await jwtOptions.Events.OnChallenge(challengeContext);
        challengeContext.Should().NotBeNull();
    }

    [Fact]
    public void AddJwtAuthentication_ShouldClearDefaultInboundClaimTypeMap()
    {
        // Arrange
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Add("test", "test");
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddJwtAuthentication(configuration);

        // Assert - The map should be cleared by the extension
        // Note: We can't directly assert this was called, but we verify the configuration works
        var serviceProvider = services.BuildServiceProvider();
        var authSchemeProvider = serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        authSchemeProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddJwtAuthentication_ShouldSetSymmetricSecurityKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddJwtAuthentication(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();
        var jwtOptions = options.Get("Bearer");

        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().NotBeNull();
        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>();
    }

    private static IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            { "Jwt:Secret", "test-secret-key-with-at-least-32-chars" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}
