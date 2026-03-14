using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGateway.Constants;
using ApiGateway.Extensions;
using FluentAssertions;
using Gateway.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.Tests.Extensions;

public class AuthExtensionTests
{
    private static JwtBearerOptions BuildJwtBearerOptions(IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        configuration ??= TestConfigurationHelper.CreateJwtConfiguration();
        services.AddJwtAuthentication(configuration);
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
    }

    private static AuthenticationScheme CreateBearerScheme()
    {
        return new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler));
    }

    [Fact]
    public void AddJwtAuthentication_WithValidConfiguration_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddJwtAuthentication(TestConfigurationHelper.CreateJwtConfiguration());

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IAuthenticationSchemeProvider>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddJwtAuthentication_WithMissingConfiguration_ShouldThrowException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddJwtAuthentication(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT configuration is missing");
    }

    [Fact]
    public void AddJwtAuthentication_ShouldConfigureTokenValidationParameters()
    {
        var jwtOptions = BuildJwtBearerOptions();

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
    public void AddJwtAuthentication_ShouldSetSymmetricSecurityKey()
    {
        var jwtOptions = BuildJwtBearerOptions();

        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().NotBeNull();
        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>();
    }

    [Fact]
    public void AddJwtAuthentication_ShouldClearDefaultInboundClaimTypeMap()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["test_clear"] = "test_clear";

        var services = new ServiceCollection();
        services.AddJwtAuthentication(TestConfigurationHelper.CreateJwtConfiguration());

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Should().BeEmpty();
    }

    [Fact]
    public void AddJwtAuthentication_ShouldRegisterEventHandlers()
    {
        var jwtOptions = BuildJwtBearerOptions();

        jwtOptions.Events.Should().NotBeNull();
        jwtOptions.Events.OnAuthenticationFailed.Should().NotBeNull();
        jwtOptions.Events.OnTokenValidated.Should().NotBeNull();
        jwtOptions.Events.OnChallenge.Should().NotBeNull();
    }

    [Fact]
    public void AddAuthorizationPolicies_ShouldRegisterAuthenticatedUserPolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationPolicies();

        var serviceProvider = services.BuildServiceProvider();
        var authOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var policy = authOptions.Value.GetPolicy(Policies.AUTHENTICATED_USER);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnAuthenticationFailed_ShouldCompleteWithoutError()
    {
        var jwtOptions = BuildJwtBearerOptions();
        var context = new AuthenticationFailedContext(new DefaultHttpContext(), CreateBearerScheme(), jwtOptions)
        {
            Exception = new SecurityTokenExpiredException("Token expired")
        };

        Func<Task> act = async () => await jwtOptions.Events.OnAuthenticationFailed(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnTokenValidated_ShouldTransformClaims()
    {
        var jwtOptions = BuildJwtBearerOptions();
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new TokenValidatedContext(new DefaultHttpContext(), CreateBearerScheme(), jwtOptions)
        {
            Principal = new ClaimsPrincipal(identity)
        };

        await jwtOptions.Events.OnTokenValidated(context);

        identity.FindFirst("role")?.Value.Should().Be("ADMIN");
        identity.FindFirst("email")?.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task OnChallenge_ShouldCompleteWithoutError()
    {
        var jwtOptions = BuildJwtBearerOptions();
        var authProperties = new AuthenticationProperties();
        var context = new JwtBearerChallengeContext(
            new DefaultHttpContext(), CreateBearerScheme(), jwtOptions, authProperties);

        Func<Task> act = async () => await jwtOptions.Events.OnChallenge(context);

        await act.Should().NotThrowAsync();
    }
}