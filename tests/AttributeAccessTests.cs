namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class AttributeAccessTests {
  [Fact]
  public void AttributeAccess_ShouldAccessObjectProperties() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ user.name }} is {{ user.age }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["user"] = new Dictionary<string, object?> { ["name"] = "Alice", ["age"] = 30 }
    });

    // Assert
    result.Should().Be("Alice is 30");
  }

  [Fact]
  public void IndexAccess_ShouldAccessArrayElements() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ items[0] }} {{ items[1] }} {{ items[-1] }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["items"] = new[] { "a", "b", "c" } });

    // Assert
    result.Should().Be("a b c");
  }
}
