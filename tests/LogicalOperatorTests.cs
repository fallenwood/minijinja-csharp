namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class LogicalOperatorTests {
  [Theory]
  [InlineData("{{ true and true }}", "true")]
  [InlineData("{{ true and false }}", "false")]
  [InlineData("{{ false or true }}", "true")]
  [InlineData("{{ false or false }}", "false")]
  [InlineData("{{ not true }}", "false")]
  [InlineData("{{ not false }}", "true")]
  public void LogicalOperators_ShouldEvaluateCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be(expected);
  }
}
