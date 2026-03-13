using System.Text;
using ApiGateway.Constants;
using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.Tests.Extensions;

public class AuthExtensionTests
{
    [Fact]
    public void AddJwtAuthentication_WithValidConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        // Act
        services.AddJwtAuthentication(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authenticationSchemeProvider = serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        authenticationSchemeProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddJwtAuthentication_WithMissingConfiguration_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddJwtAuthentication(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT configuration is missing");
    }

    [Fact]
    public void AddJwtAuthentication_ShouldConfigureJwtBearerOptions()
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

        jwtOptions.Should().NotBeNull();
        jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be("test-issuer");
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be("test-audience");
        jwtOptions.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        jwtOptions.TokenValidationParameters.RoleClaimType.Should().Be("role");
        jwtOptions.TokenValidationParameters.NameClaimType.Should().Be("name");
    }

    [Fact]
    public void AddAuthorizationPolicies_ShouldRegisterAllPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAuthorizationPolicies();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();

        authorizationOptions.Value.GetPolicy(Policies.AUTHENTICATED_USER).Should().NotBeNull();
    }

    [Fact]
    public void AddAuthorizationPolicies_AuthenticatedUserPolicy_ShouldRequireAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationPolicies();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
        var policy = authorizationOptions.Value.GetPolicy(Policies.AUTHENTICATED_USER);

        // Assert
        policy.Should().NotBeNull();
        policy!.Requirements.Should().HaveCount(1);
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