using System.Security.Claims;
using ApiGateway.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Gateway.Tests.Extensions;

public class ClaimTransformationExtensionTests
{
    private static TokenValidatedContext CreateTokenValidatedContext(ClaimsPrincipal? principal)
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();

        return new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = principal
        };
    }

    [Fact]
    public void TransformClaims_WithNullPrincipal_ShouldNotThrow()
    {
        var context = CreateTokenValidatedContext(null);

        var act = () => context.TransformClaims();

        act.Should().NotThrow();
    }

    [Fact]
    public void TransformClaims_WithNonClaimsIdentity_ShouldNotThrow()
    {
        var genericIdentity = new Mock<System.Security.Principal.IIdentity>().Object;
        var principal = new ClaimsPrincipal(genericIdentity);
        var context = CreateTokenValidatedContext(principal);

        var act = () => context.TransformClaims();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "ADMIN", "role")]
    [InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "test@example.com", "email")]
    [InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "user-123", "sub")]
    public void TransformClaims_WithLongFormClaim_ShouldTransformToShortForm(
        string originalType, string value, string expectedShortType)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(originalType, value) }, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));

        context.TransformClaims();

        identity.FindFirst(expectedShortType)?.Value.Should().Be(value);
        identity.FindFirst(originalType).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesRole_ShouldTransformToShortRole()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "MANAGER") }, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));

        context.TransformClaims();

        identity.FindFirst("role")?.Value.Should().Be("MANAGER");
        identity.FindFirst(ClaimTypes.Role).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesEmail_ShouldTransformToShortEmail()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "user@domain.com") }, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));

        context.TransformClaims();

        identity.FindFirst("email")?.Value.Should().Be("user@domain.com");
        identity.FindFirst(ClaimTypes.Email).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithClaimTypesNameIdentifier_ShouldTransformToSub()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user-456") }, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));

        context.TransformClaims();

        identity.FindFirst("sub")?.Value.Should().Be("user-456");
        identity.FindFirst(ClaimTypes.NameIdentifier).Should().BeNull();
    }

    [Fact]
    public void TransformClaims_WithAllClaimTypes_ShouldTransformAll()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Email, "admin@test.com"),
            new Claim(ClaimTypes.NameIdentifier, "admin-789")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));

        context.TransformClaims();

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
        var claims = new[]
        {
            new Claim("custom-claim", "custom-value"),
            new Claim("another-claim", "another-value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = CreateTokenValidatedContext(new ClaimsPrincipal(identity));
        var originalClaimCount = identity.Claims.Count();

        context.TransformClaims();

        identity.Claims.Should().HaveCount(originalClaimCount);
        identity.FindFirst("custom-claim")?.Value.Should().Be("custom-value");
        identity.FindFirst("another-claim")?.Value.Should().Be("another-value");
    }
}