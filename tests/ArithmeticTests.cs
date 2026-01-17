namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class ArithmeticTests {
  [Theory]
  [InlineData("{{ 1 + 2 }}", "3")]
  [InlineData("{{ 10 - 3 }}", "7")]
  [InlineData("{{ 4 * 5 }}", "20")]
  [InlineData("{{ 10 / 4 }}", "2.5")]
  [InlineData("{{ 10 // 4 }}", "2")]
  [InlineData("{{ 10 % 3 }}", "1")]
  [InlineData("{{ 2 ** 3 }}", "8")]
  [InlineData("{{ -5 }}", "-5")]
  public void Arithmetic_ShouldComputeCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be(expected);
  }
}
