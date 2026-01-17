namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class InOperatorTests {
  [Theory]
  [InlineData("{{ 'a' in 'abc' }}", "true")]
  [InlineData("{{ 'd' in 'abc' }}", "false")]
  [InlineData("{{ 1 in [1,2,3] }}", "true")]
  [InlineData("{{ 4 in [1,2,3] }}", "false")]
  public void InOperator_ShouldCheckContainment(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be(expected);
  }
}
