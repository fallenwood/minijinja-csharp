namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class FilterTests {
  [Theory]
  [InlineData("{{ 'hello'|upper }}", "HELLO")]
  [InlineData("{{ 'HELLO'|lower }}", "hello")]
  [InlineData("{{ 'hello'|capitalize }}", "Hello")]
  [InlineData("{{ '  hello  '|trim }}", "hello")]
  [InlineData("{{ [1,2,3]|length }}", "3")]
  [InlineData("{{ [1,2,3]|first }}", "1")]
  [InlineData("{{ [1,2,3]|last }}", "3")]
  [InlineData("{{ [3,1,2]|sort|join(',') }}", "1,2,3")]
  [InlineData("{{ [1,2,3]|join('-') }}", "1-2-3")]
  [InlineData("{{ 'hello'|replace('l','x') }}", "hexxo")]
  [InlineData("{{ 5|abs }}", "5")]
  [InlineData("{{ -5|abs }}", "5")]
  [InlineData("{{ 3.7|int }}", "3")]
  [InlineData("{{ 3|float }}", "3.0")]
  public void Filters_ShouldTransformValuesCorrectly(string template, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(expected);
  }

  [Fact]
  public void DefaultFilter_WhenValueUndefined_ShouldReturnFallback() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x|default('fallback') }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("fallback");
  }

  [Fact]
  public void DefaultFilter_WhenValueDefined_ShouldReturnValue() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x|default('fallback') }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["x"] = "value" });

    // Assert
    result.Should().Be("value");
  }
}
