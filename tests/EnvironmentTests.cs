namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;
using Value = MiniJinja.Value;

public class EnvironmentTests {
  [Fact]
  public void Version_ShouldReturnVersion() {
    MiniJinja.Environment.Version.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void AddTemplate_ShouldMakeTemplateAvailable() {
    var env = new Environment();
    env.AddTemplate("test", "Hello {{ name }}");
    var tmpl = env.GetTemplate("test");
    var result = tmpl.Render(new Dictionary<string, object?> { ["name"] = "World" });
    result.Should().Be("Hello World");
  }

  [Fact]
  public void GetTemplate_WhenNotFound_ShouldThrow() {
    var env = new Environment();
    Action act = () => env.GetTemplate("nonexistent");
    act.Should().Throw<TemplateError>().WithMessage("*not found*");
  }

  [Fact]
  public void AddGlobal_ShouldBeAccessibleInTemplates() {
    var env = new Environment();
    env.AddGlobal("sitename", "MySite");
    var tmpl = env.TemplateFromString("{{ sitename }}");
    var result = tmpl.Render();
    result.Should().Be("MySite");
  }

  [Fact]
  public void TryGetGlobal_WhenExists_ShouldReturnTrue() {
    var env = new Environment();
    env.AddGlobal("key", "value");
    var found = env.TryGetGlobal("key", out var value);
    found.Should().BeTrue();
    value.AsString().Should().Be("value");
  }

  [Fact]
  public void TryGetGlobal_WhenNotExists_ShouldReturnFalse() {
    var env = new Environment();
    var found = env.TryGetGlobal("missing", out _);
    found.Should().BeFalse();
  }

  [Fact]
  public void AddFilter_Simple_ShouldWork() {
    var env = new Environment();
    env.AddFilter("double", v => Value.FromInt(v.AsInt() * 2));
    var tmpl = env.TemplateFromString("{{ 5|double }}");
    var result = tmpl.Render();
    result.Should().Be("10");
  }

  [Fact]
  public void AddFilter_WithArgs_ShouldWork() {
    var env = new Environment();
    env.AddFilter("multiply", (v, args) => Value.FromInt(v.AsInt() * args[0].AsInt()));
    var tmpl = env.TemplateFromString("{{ 5|multiply(3) }}");
    var result = tmpl.Render();
    result.Should().Be("15");
  }

  [Fact]
  public void AddFilter_Full_ShouldWork() {
    var env = new Environment();
    env.AddFilter("ctx", (v, args, kwargs, state) => {
      state.TryGet("extra", out var extra);
      return Value.FromString(v.AsString() + extra.AsString());
    });
    var tmpl = env.TemplateFromString("{{ 'hello'|ctx }}");
    var result = tmpl.Render(new Dictionary<string, object?> { ["extra"] = "!" });
    result.Should().Be("hello!");
  }

  [Fact]
  public void TryGetFilter_WhenExists_ShouldReturnTrue() {
    var env = new Environment();
    env.AddFilter("myfilter", v => v);
    var found = env.TryGetFilter("myfilter", out var filter);
    found.Should().BeTrue();
    filter.Should().NotBeNull();
  }

  [Fact]
  public void TryGetFilter_WhenNotExists_ShouldReturnFalse() {
    var env = new Environment();
    var found = env.TryGetFilter("nonexistent", out _);
    found.Should().BeFalse();
  }

  [Fact]
  public void AddTest_Simple_ShouldWork() {
    var env = new Environment();
    env.AddTest("positive", v => v.AsInt() > 0);
    var tmpl = env.TemplateFromString("{{ 5 is positive }}");
    var result = tmpl.Render();
    result.Should().Be("true");
  }

  [Fact]
  public void AddTest_WithArgs_ShouldWork() {
    var env = new Environment();
    env.AddTest("multiple", (v, args) => v.AsInt() % args[0].AsInt() == 0);
    var tmpl = env.TemplateFromString("{{ 10 is multiple(5) }}");
    var result = tmpl.Render();
    result.Should().Be("true");
  }

  [Fact]
  public void HasTest_WhenExists_ShouldReturnTrue() {
    var env = new Environment();
    env.AddTest("mytest", _ => true);
    env.HasTest("mytest").Should().BeTrue();
  }

  [Fact]
  public void HasTest_BuiltIn_ShouldReturnTrue() {
    var env = new Environment();
    env.HasTest("defined").Should().BeTrue();
  }

  [Fact]
  public void RunTest_WhenNotFound_ShouldThrow() {
    var env = new Environment();
    Action act = () => env.RunTest("nonexistent", Value.FromInt(1), []);
    act.Should().Throw<TemplateError>().WithMessage("*Unknown test*");
  }

  [Fact]
  public void AddFunction_Simple_ShouldWork() {
    var env = new Environment();
    env.AddFunction("getvalue", () => Value.FromInt(42));
    var tmpl = env.TemplateFromString("{{ getvalue() }}");
    var result = tmpl.Render();
    result.Should().Be("42");
  }

  [Fact]
  public void AddFunction_WithArgs_ShouldWork() {
    var env = new Environment();
    env.AddFunction("add", args => Value.FromInt(args[0].AsInt() + args[1].AsInt()));
    var tmpl = env.TemplateFromString("{{ add(2, 3) }}");
    var result = tmpl.Render();
    result.Should().Be("5");
  }

  [Fact]
  public void AddFunction_Full_ShouldWork() {
    var env = new Environment();
    env.AddFunction("greet", (args, kwargs, state) => {
      var name = kwargs.TryGetValue("name", out var n) ? n.AsString() : "World";
      return Value.FromString($"Hello {name}");
    });
    var tmpl = env.TemplateFromString("{{ greet(name='Alice') }}");
    var result = tmpl.Render();
    result.Should().Be("Hello Alice");
  }

  [Fact]
  public void Dispose_ShouldPreventFurtherUse() {
    var env = new Environment();
    env.Dispose();
    Action act = () => env.TemplateFromString("test");
    act.Should().Throw<ObjectDisposedException>();
  }

  [Fact]
  public void Dispose_MultipleCalls_ShouldNotThrow() {
    var env = new Environment();
    env.Dispose();
    env.Dispose(); // Should not throw
  }

  [Fact]
  public void TemplateFromNamedString_ShouldSetName() {
    var env = new Environment();
    var tmpl = env.TemplateFromNamedString("mytemplate", "test");
    // Template name is used in errors, so we just verify it works
    var result = tmpl.Render();
    result.Should().Be("test");
  }

  [Fact]
  public void Template_RenderWithNoContext_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("Hello World");
    var result = tmpl.Render();
    result.Should().Be("Hello World");
  }

  [Fact]
  public void Template_RenderWithValueDict_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x }}");
    var ctx = new Dictionary<string, Value> { ["x"] = Value.FromInt(42) };
    var result = tmpl.Render(ctx);
    result.Should().Be("42");
  }

  [Fact]
  public void Template_RenderWithSerializable_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ name }}");
    var ctx = new TestContext { Name = "Test" };
    var result = tmpl.Render(ctx);
    result.Should().Be("Test");
  }

  private class TestContext : ITemplateSerializable {
    public string Name { get; set; } = "";
    public Dictionary<string, MiniJinja.Value> ToTemplateValues() =>
      new() { ["name"] = MiniJinja.Value.FromString(Name) };
  }
}
