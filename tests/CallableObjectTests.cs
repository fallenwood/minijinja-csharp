namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class CallableObjectTests {
  [Fact]
  public void Cycler_ShouldCycleThroughValues() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"{% set c = cycler(""odd"", ""even"") %}{{ c.next() }} {{ c.next() }} {{ c.next() }}");

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be("odd even odd");
  }

  [Fact]
  public void Joiner_ShouldJoinWithSeparator() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"{% set j = joiner("", "") %}{% for item in items %}{{ j() }}{{ item }}{% endfor %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["items"] = new[] { "a", "b", "c" } });

    // Assert
    result.Should().Be("a, b, c");
  }
}
