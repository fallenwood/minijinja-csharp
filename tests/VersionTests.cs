namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class VersionTests {
  [Fact]
  public void Version_ShouldNotBeEmpty() {
    // Arrange & Act
    var version = Environment.Version;

    // Assert
    version.Should().NotBeNullOrEmpty();
  }
}
