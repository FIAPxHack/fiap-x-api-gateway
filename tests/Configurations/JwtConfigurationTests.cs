using ApiGateway.Configurations;
using FluentAssertions;
using Xunit;

namespace Gateway.Tests.Configurations;

public class JwtConfigurationTests
{
    [Fact]
    public void Validate_WithAllPropertiesSet_ShouldNotThrow()
    {
        // Arrange
        var config = new JwtConfiguration
        {
            Secret = "test-secret-key",
            Issuer = "test-issuer",
            Audience = "test-audience"
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "issuer", "audience", "JWT Secret is required")]
    [InlineData("   ", "issuer", "audience", "JWT Secret is required")]
    [InlineData(null, "issuer", "audience", "JWT Secret is required")]
    [InlineData("secret", "", "audience", "JWT Issuer is required")]
    [InlineData("secret", "   ", "audience", "JWT Issuer is required")]
    [InlineData("secret", null, "audience", "JWT Issuer is required")]
    [InlineData("secret", "issuer", "", "JWT Audience is required")]
    [InlineData("secret", "issuer", "   ", "JWT Audience is required")]
    [InlineData("secret", "issuer", null, "JWT Audience is required")]
    public void Validate_WithMissingProperty_ShouldThrowInvalidOperationException(
        string secret, string issuer, string audience, string expectedMessage)
    {
        // Arrange
        var config = new JwtConfiguration
        {
            Secret = secret,
            Issuer = issuer,
            Audience = audience
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void Properties_ShouldHaveDefaultEmptyValues()
    {
        // Arrange & Act
        var config = new JwtConfiguration();

        // Assert
        config.Secret.Should().BeEmpty();
        config.Issuer.Should().BeEmpty();
        config.Audience.Should().BeEmpty();
    }
}