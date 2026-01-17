namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class LoopRecursionTests {
  [Fact]
  public void LoopRecursion_ShouldHandleNestedItems() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items recursive %}{{ item.name }}{% if item.children %}({{ loop(item.children) }}){% endif %}{% endfor %}");

    var items = new List<Dictionary<string, object?>>
    {
      new()
      {
        ["name"] = "A",
        ["children"] = new List<Dictionary<string, object?>>
        {
          new() { ["name"] = "A1", ["children"] = null },
          new() { ["name"] = "A2", ["children"] = null }
        }
      },
      new() { ["name"] = "B", ["children"] = null }
    };

    // Act
    var result = tmpl.Render(new { items });

    // Assert
    result.Should().Be("A(A1A2)B");
  }
}
