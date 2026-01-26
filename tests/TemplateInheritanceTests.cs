namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class TemplateInheritanceTests {
  [Fact]
  public void Extends_ShouldInheritFromBase() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("base.html", "<html>{% block content %}default{% endblock %}</html>");
    var tmpl = env.TemplateFromString(@"{% extends ""base.html"" %}{% block content %}Hello World{% endblock %}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("<html>Hello World</html>");
  }

  [Fact]
  public void ExtendsWithSuper_ShouldIncludeParentContent() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("base.html", "{% block content %}BASE{% endblock %}");
    var tmpl = env.TemplateFromString(@"{% extends ""base.html"" %}{% block content %}{{ super() }}:CHILD{% endblock %}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("BASE:CHILD");
  }

  [Fact]
  public void ExtendsMultipleLevels_ShouldInheritCorrectly() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("base.html", "[{% block content %}BASE{% endblock %}]");
    env.AddTemplate("middle.html", @"{% extends ""base.html"" %}{% block content %}MIDDLE{% endblock %}");
    var tmpl = env.TemplateFromString(@"{% extends ""middle.html"" %}{% block content %}CHILD{% endblock %}");

    // Act
    var result = tmpl.Render();

    // Assert
    result.Should().Be("[CHILD]");
  }

  [Fact]
  public void ExtendsWithVariable_ShouldPassContextToChild() {
    // Arrange
    var env = new Environment();
    env.AddTemplate("base.html", "Hello {% block name %}World{% endblock %}!");
    var tmpl = env.TemplateFromString(@"{% extends ""base.html"" %}{% block name %}{{ name }}{% endblock %}");

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["name"] = "Alice" });

    // Assert
    result.Should().Be("Hello Alice!");
  }
}
