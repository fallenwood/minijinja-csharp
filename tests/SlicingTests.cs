namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class SlicingTests {
  [Theory]
  [InlineData("{{ 'hello'[1:3] }}", "el")]
  [InlineData("{{ 'hello'[:3] }}", "hel")]
  [InlineData("{{ 'hello'[2:] }}", "llo")]
  [InlineData("{{ [1,2,3,4,5][1:4] }}", "[2, 3, 4]")]
  public void Slicing_ShouldSliceCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(expected);
  }
}
