namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class SetStatementTests {
  [Fact]
  public void SetStatement_ShouldAssignVariable() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set x = 5 %}{{ x }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("5");
  }
}
