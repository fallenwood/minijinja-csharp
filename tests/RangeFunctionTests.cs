namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class RangeFunctionTests {
  [Theory]
  [InlineData("{% for i in range(3) %}{{ i }}{% endfor %}", "012")]
  [InlineData("{% for i in range(1, 4) %}{{ i }}{% endfor %}", "123")]
  [InlineData("{% for i in range(0, 6, 2) %}{{ i }}{% endfor %}", "024")]
  public void RangeFunction_ShouldGenerateSequence(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be(expected);
  }
}
