namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class TestsTests {
  [Fact]
  public void IsDefined_WhenDefined_ShouldReturnTrue() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x is defined }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["x"] = 1 });

    // Assert
    result.Should().Be("true");
  }

  [Fact]
  public void IsDefined_WhenUndefined_ShouldReturnFalse() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ y is defined }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["x"] = 1 });

    // Assert
    result.Should().Be("false");
  }

  [Theory]
  [InlineData("{{ 3 is odd }}", "true")]
  [InlineData("{{ 4 is even }}", "true")]
  [InlineData("{{ 10 is divisibleby(5) }}", "true")]
  [InlineData("{{ 10 is divisibleby(3) }}", "false")]
  public void NumericTests_ShouldEvaluateCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(expected);
  }
}
