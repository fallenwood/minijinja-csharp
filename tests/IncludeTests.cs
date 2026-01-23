namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class IncludeTests {
  [Fact]
  public void Include_ShouldIncludeTemplate() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("header.html", "Header: {{ title }}");
    var tmpl = env.TemplateFromString(@"{% include ""header.html"" %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["title"] = "Welcome" });

    // Assert
    result.Should().Be("Header: Welcome");
  }
}
