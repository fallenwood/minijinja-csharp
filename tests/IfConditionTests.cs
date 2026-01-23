namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class IfConditionTests {
  [Fact]
  public void IfCondition_WhenTrue_ShouldRenderContent() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% if show %}visible{% endif %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["show"] = true });

    // Assert
    result.Should().Be("visible");
  }

  [Fact]
  public void IfCondition_WhenFalse_ShouldRenderNothing() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% if show %}visible{% endif %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["show"] = false });

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void IfElse_WhenTrue_ShouldRenderIfBranch() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% if show %}yes{% else %}no{% endif %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["show"] = true });

    // Assert
    result.Should().Be("yes");
  }

  [Fact]
  public void IfElse_WhenFalse_ShouldRenderElseBranch() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% if show %}yes{% else %}no{% endif %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["show"] = false });

    // Assert
    result.Should().Be("no");
  }

  [Theory]
  [InlineData(1, "one")]
  [InlineData(2, "two")]
  [InlineData(3, "other")]
  public void IfElif_ShouldRenderCorrectBranch(int x, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% if x == 1 %}one{% elif x == 2 %}two{% else %}other{% endif %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["x"] = x });

    // Assert
    result.Should().Be(expected);
  }
}
