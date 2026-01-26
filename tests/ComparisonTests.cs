namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class ComparisonTests {
  [Theory]
  [InlineData("{{ 1 == 1 }}", "true")]
  [InlineData("{{ 1 != 2 }}", "true")]
  [InlineData("{{ 1 < 2 }}", "true")]
  [InlineData("{{ 2 > 1 }}", "true")]
  [InlineData("{{ 1 <= 1 }}", "true")]
  [InlineData("{{ 2 >= 2 }}", "true")]
  [InlineData("{{ 1 == 2 }}", "false")]
  public void Comparisons_ShouldEvaluateCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(expected);
  }
}
