namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class ForLoopTests {
  [Fact]
  public void ForLoop_ShouldIterateOverItems() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items %}{{ item }}{% endfor %}");

    // Act
    var result = tmpl.Render(new { items = new[] { "a", "b", "c" } });

    // Assert
    result.Should().Be("abc");
  }

  [Fact]
  public void ForLoop_WithIndex_ShouldShowLoopIndex() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items %}{{ loop.index }}:{{ item }} {% endfor %}");

    // Act
    var result = tmpl.Render(new { items = new[] { "a", "b", "c" } });

    // Assert
    result.Should().Be("1:a 2:b 3:c ");
  }

  [Fact]
  public void ForLoop_WithElse_ShouldRenderElseWhenEmpty() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items %}{{ item }}{% else %}empty{% endfor %}");

    // Act
    var result = tmpl.Render(new { items = Array.Empty<string>() });

    // Assert
    result.Should().Be("empty");
  }

  [Fact]
  public void NestedForLoop_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for row in rows %}{% for col in row %}{{ col }}{% endfor %},{% endfor %}");

    // Act
    var result = tmpl.Render(new { rows = new[] { new[] { 1, 2 }, new[] { 3, 4 } } });

    // Assert
    result.Should().Be("12,34,");
  }

  [Fact]
  public void ForLoop_WithFilter_ShouldFilterItems() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for x in items if x > 2 %}{{ x }}{% endfor %}");

    // Act
    var result = tmpl.Render(new { items = new[] { 1, 2, 3, 4, 5 } });

    // Assert
    result.Should().Be("345");
  }
}
