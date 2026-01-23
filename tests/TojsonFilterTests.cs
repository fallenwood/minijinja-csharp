namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class TojsonFilterTests {
  [Theory]
  [InlineData("hello", @"""hello""")]
  public void TojsonFilter_ShouldSerializeString(string value, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ value|tojson }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["value"] = value });

    // Assert
    result.Should().Be(expected);
  }

  [Fact]
  public void TojsonFilter_ShouldSerializeNumber() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ value|tojson }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["value"] = 42 });

    // Assert
    result.Should().Be("42");
  }

  [Fact]
  public void TojsonFilter_ShouldSerializeArray() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ value|tojson }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["value"] = new[] { 1, 2, 3 } });

    // Assert
    result.Should().Be("[1,2,3]");
  }
}
