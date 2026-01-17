namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class MacroTests {
  [Fact]
  public void Macro_ShouldDefineAndCall() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"{% macro greet(name) %}Hello {{ name }}!{% endmacro %}{{ greet(""World"") }}");

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be("Hello World!");
  }

  [Fact]
  public void MacroWithDefault_ShouldUseDefaultValue() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"{% macro greet(name=""Guest"") %}Hello {{ name }}!{% endmacro %}{{ greet() }}");

    // Act
    var result = tmpl.Render(null);

    // Assert
    result.Should().Be("Hello Guest!");
  }
}
