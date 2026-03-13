using ApiGateway.Constants;
using FluentAssertions;
using Xunit;

namespace Gateway.Tests.Constants;

public class PoliciesTests
{
    [Theory]
    [InlineData("AUTHENTICATED_USER", "AuthenticatedUser")]
    public void AllPolicies_ShouldHaveCorrectValues(string fieldName, string expectedValue)
    {
        // Act & Assert
        typeof(Policies).GetField(fieldName)?.GetValue(null)
            .Should().Be(expectedValue);
    }

    [Fact]
    public void Policies_ShouldHaveExactlyOneConstant()
    {
        // Arrange & Act
        var policyFields = typeof(Policies).GetFields();

        // Assert
        policyFields.Should().HaveCount(1);
    }
}