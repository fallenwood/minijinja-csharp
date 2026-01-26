namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class ImportTests {
  [Fact]
  public void Import_ShouldImportMacros() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("forms.html", @"{% macro input(name) %}<input name=""{{ name }}"">{% endmacro %}");
    var tmpl = env.TemplateFromString(@"{% import ""forms.html"" as forms %}{{ forms.input(""test"") }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(@"<input name=""test"">");
  }

  [Fact]
  public void FromImport_ShouldImportSpecificMacros() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("forms.html", @"{% macro input(name) %}<input name=""{{ name }}"">{% endmacro %}{% macro button(text) %}<button>{{ text }}</button>{% endmacro %}");
    var tmpl = env.TemplateFromString(@"{% from ""forms.html"" import input, button %}{{ input(""test"") }}{{ button(""Click"") }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(@"<input name=""test""><button>Click</button>");
  }

  [Fact]
  public void FromImportWithAlias_ShouldImportWithAlias() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("forms.html", @"{% macro input(name) %}<input name=""{{ name }}"">{% endmacro %}");
    var tmpl = env.TemplateFromString(@"{% from ""forms.html"" import input as inp %}{{ inp(""test"") }}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be(@"<input name=""test"">");
  }
}
