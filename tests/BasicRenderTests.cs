namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class BasicRenderTests {
  [Fact]
  public void BasicRender_ShouldRenderHelloWorld() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("Hello {{ name }}!");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["name"] = "World" });

    // Assert
    result.Should().Be("Hello World!");
  }

  [Fact]
  public void VariableTypes_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ str }} {{ num }} {{ floatVal }} {{ boolVal }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["str"] = "hello",
      ["num"] = 42,
      ["floatVal"] = 3.14,
      ["boolVal"] = true
    });

    // Assert
    result.Should().Be("hello 42 3.14 true");
  }
}
