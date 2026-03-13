using System.Security.Claims;
using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Gateway.Tests.Extensions;

public class ClaimTransformationExtensionTests
{
    [Fact]
    public void TransformClaims_WithNullPrincipal_ShouldNotThrow()
    {
        // Arrange
        var context = CreateTokenValidatedContext(null);

        // Act
        var act = () => context.TransformClaims();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TransformClaims_WithNonClaimsIdentity_ShouldNotThrow()
    {
        // Arrange
        var genericIdentity = new Mock<System.Security.Principal.IIdentity>().Object;
        var principal = new ClaimsPrincipal(genericIdentity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        var act = () => context.TransformClaims();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TransformClaims_WithMicrosoftRoleClaim_ShouldTransformToShortRole()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("role")?.Value.Should().Be("ADMIN");
        identity.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesRole_ShouldTransformToShortRole()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "MANAGER")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("role")?.Value.Should().Be("MANAGER");
        identity.FindFirst(ClaimTypes.Role).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithMicrosoftEmailClaim_ShouldTransformToShortEmail()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("email")?.Value.Should().Be("test@example.com");
        identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
            .Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesEmail_ShouldTransformToShortEmail()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "user@domain.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("email")?.Value.Should().Be("user@domain.com");
        identity.FindFirst(ClaimTypes.Email).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithMicrosoftNameIdentifier_ShouldTransformToSub()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "user-123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("sub")?.Value.Should().Be("user-123");
        identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            .Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesNameIdentifier_ShouldTransformToSub()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-456")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("sub")?.Value.Should().Be("user-456");
        identity.FindFirst(ClaimTypes.NameIdentifier).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithAllClaimTypes_ShouldTransformAll()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.NameIdentifier, "admin-789")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);

        // Act
        context.TransformClaims();

        // Assert
        identity.FindFirst("role")?.Value.Should().Be("ADMIN");
        identity.FindFirst("email")?.Value.Should().Be("admin@test.com");
        identity.FindFirst("sub")?.Value.Should().Be("admin-789");
        identity.FindFirst(ClaimTypes.Role).Should().BeNull();
        identity.FindFirst(ClaimTypes.Email).Should().BeNull();
        identity.FindFirst(ClaimTypes.NameIdentifier).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithNoRelevantClaims_ShouldNotModifyIdentity()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("custom-claim", "custom-value"),
            new Claim("another-claim", "another-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateTokenValidatedContext(principal);
        var originalClaimCount = identity.Claims.Count();

        // Act
        context.TransformClaims();

        // Assert
        identity.Claims.Should().HaveCount(originalClaimCount);
        identity.FindFirst("custom-claim")?.Value.Should().Be("custom-value");
        identity.FindFirst("another-claim")?.Value.Should().Be("another-value");
    }

    private static TokenValidatedContext CreateTokenValidatedContext(ClaimsPrincipal? principal)
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new Microsoft.AspNetCore.Authentication.AuthenticationScheme(
            "Bearer",
            "Bearer",
            typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();
        
        return new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = principal
        };
    }
}