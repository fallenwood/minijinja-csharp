namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class WithBlockTests {
  [Fact]
  public void WithBlock_ShouldCreateLocalScope() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% with x = 5, y = 10 %}{{ x + y }}{% endwith %}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("15");
  }
}
