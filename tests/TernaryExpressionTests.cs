namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class TernaryExpressionTests {
  [Fact]
  public void TernaryExpression_WhenTrue_ShouldReturnTrueValue() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'yes' if x else 'no' }}");

    // Act
    var result = tmpl.Render(new { x = true });

    // Assert
    result.Should().Be("yes");
  }

  [Fact]
  public void TernaryExpression_WhenFalse_ShouldReturnFalseValue() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'yes' if x else 'no' }}");

    // Act
    var result = tmpl.Render(new { x = false });

    // Assert
    result.Should().Be("no");
  }
}
