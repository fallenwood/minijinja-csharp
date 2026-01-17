namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class StringConcatTests {
  [Fact]
  public void StringConcat_ShouldConcatenateStrings() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'hello' ~ ' ' ~ 'world' }}");

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be("hello world");
  }
}
