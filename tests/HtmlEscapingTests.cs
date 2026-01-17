namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class HtmlEscapingTests {
  [Fact]
  public void HtmlEscaping_ShouldEscapeHtmlCharacters() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromNamedString("test.html", "{{ content }}");

    // Act
    var result = tmpl.Render(new { content = "<script>alert('xss')</script>" });

    // Assert
    result.Should().Contain("&lt;");
  }

  [Fact]
  public void SafeFilter_ShouldNotEscapeContent() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromNamedString("test.html", "{{ content|safe }}");

    // Act
    var result = tmpl.Render(new { content = "<b>bold</b>" });

    // Assert
    result.Should().Be("<b>bold</b>");
  }
}
