namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class UrlencodeFilterTests {
  [Theory]
  [InlineData("hello world", "hello%20world")]
  [InlineData("a=b&c=d", "a%3Db%26c%3Dd")]
  public void UrlencodeFilter_ShouldEncodeString(string value, string expected) {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ value|urlencode }}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["value"] = value });

    // Assert
    result.Should().Be(expected);
  }
}
