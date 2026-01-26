namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class DictLiteralTests {
  [Fact]
  public void DictLiteral_ShouldAccessProperty() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ {'a': 1, 'b': 2}.a }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("1");
  }
}
