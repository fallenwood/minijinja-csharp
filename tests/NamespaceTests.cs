namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class NamespaceTests {
  [Fact]
  public void Namespace_ShouldAllowMutableState() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set ns = namespace(count=0) %}{% for i in range(3) %}{% set ns.count = ns.count + 1 %}{% endfor %}{{ ns.count }}");

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be("3");
  }
}
