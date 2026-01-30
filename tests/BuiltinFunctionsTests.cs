namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;
using Value = MiniJinja.Value;

public class BuiltinFunctionsTests {
  [Theory]
  [InlineData("{% for i in range(3) %}{{ i }}{% endfor %}", "012")]
  [InlineData("{% for i in range(1, 4) %}{{ i }}{% endfor %}", "123")]
  [InlineData("{% for i in range(0, 6, 2) %}{{ i }}{% endfor %}", "024")]
  [InlineData("{% for i in range(5, 0, -1) %}{{ i }}{% endfor %}", "54321")]
  public void Range_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Range_ZeroStep_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for i in range(0, 5, 0) %}{% endfor %}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*step cannot be zero*");
  }

  [Fact]
  public void Lipsum_Default_ShouldGenerateParagraphs() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ lipsum() }}");
    var result = tmpl.Render();
    result.Should().Contain("<p>");
    result.Should().Contain("Lorem ipsum");
  }

  [Fact]
  public void Lipsum_WithCount_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ lipsum(2) }}");
    var result = tmpl.Render();
    // Should have 2 paragraphs
    var count = result.Split("<p>").Length - 1;
    count.Should().Be(2);
  }

  [Fact]
  public void Lipsum_NoHtml_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ lipsum(2, html=false) }}");
    var result = tmpl.Render();
    result.Should().NotContain("<p>");
    result.Should().Contain("\n\n");
  }

  [Fact]
  public void Cycler_ShouldCycleValues() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set c = cycler('a', 'b', 'c') %}{{ c() }}{{ c() }}{{ c() }}{{ c() }}");
    var result = tmpl.Render();
    result.Should().Be("abca");
  }

  [Fact]
  public void Cycler_Current_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set c = cycler('x', 'y') %}{{ c.current }}{{ c.next() }}{{ c.current }}");
    var result = tmpl.Render();
    result.Should().Be("xxy");
  }

  [Fact]
  public void Cycler_Reset_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set c = cycler('a', 'b') %}{{ c() }}{{ c() }}{% set _ = c.reset() %}{{ c() }}");
    var result = tmpl.Render();
    result.Should().Be("aba");
  }

  [Fact]
  public void Cycler_Empty_ShouldReturnNone() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set c = cycler() %}{{ c() }}{{ c.current }}");
    var result = tmpl.Render();
    // None renders as empty string in templates
    result.Should().Be("");
  }

  [Fact]
  public void Cycler_TryGetAttr_Unknown_ShouldReturnFalse() {
    var cycler = new Cycler([Value.FromInt(1)]);
    cycler.TryGetAttr("unknown", out var value).Should().BeFalse();
    value.IsNone.Should().BeTrue();
  }

  [Fact]
  public void Cycler_TryGetItem_ShouldReturnFalse() {
    var cycler = new Cycler([Value.FromInt(1)]);
    cycler.TryGetItem(Value.FromInt(0), out var value).Should().BeFalse();
    value.IsNone.Should().BeTrue();
  }

  [Fact]
  public void Cycler_TryIter_ShouldReturnNull() {
    var cycler = new Cycler([Value.FromInt(1)]);
    cycler.TryIter().Should().BeNull();
  }

  [Fact]
  public void Cycler_Length_ShouldReturnNull() {
    var cycler = new Cycler([Value.FromInt(1)]);
    cycler.Length.Should().BeNull();
  }

  [Fact]
  public void Joiner_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set j = joiner() %}{% for item in [1,2,3] %}{{ j() }}{{ item }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("1, 2, 3");
  }

  [Fact]
  public void Joiner_CustomSeparator_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set j = joiner(' | ') %}{% for item in ['a','b'] %}{{ j() }}{{ item }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("a | b");
  }

  [Fact]
  public void Joiner_TryGetAttr_ShouldReturnFalse() {
    var joiner = new Joiner(", ");
    joiner.TryGetAttr("anything", out var value).Should().BeFalse();
    value.IsNone.Should().BeTrue();
  }

  [Fact]
  public void Joiner_TryGetItem_ShouldReturnFalse() {
    var joiner = new Joiner(", ");
    joiner.TryGetItem(Value.FromInt(0), out var value).Should().BeFalse();
    value.IsNone.Should().BeTrue();
  }

  [Fact]
  public void Joiner_TryIter_ShouldReturnNull() {
    var joiner = new Joiner(", ");
    joiner.TryIter().Should().BeNull();
  }

  [Fact]
  public void Joiner_Length_ShouldReturnNull() {
    var joiner = new Joiner(", ");
    joiner.Length.Should().BeNull();
  }

  [Fact]
  public void Namespace_ShouldStoreValues() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set ns = namespace(x=1) %}{{ ns.x }}{% set ns.x = 2 %}{{ ns.x }}");
    var result = tmpl.Render();
    result.Should().Be("12");
  }

  [Fact]
  public void Namespace_InLoop_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set ns = namespace(sum=0) %}{% for i in range(5) %}{% set ns.sum = ns.sum + i %}{% endfor %}{{ ns.sum }}");
    var result = tmpl.Render();
    result.Should().Be("10");
  }

  [Fact]
  public void Namespace_TryGetItem_ShouldWork() {
    var ns = new Namespace();
    ns.Set("key", Value.FromInt(42));
    ns.TryGetItem(Value.FromString("key"), out var value).Should().BeTrue();
    value.AsInt().Should().Be(42);
  }

  [Fact]
  public void Namespace_TryIter_ShouldReturnNull() {
    var ns = new Namespace();
    ns.TryIter().Should().BeNull();
  }

  [Fact]
  public void Namespace_Length_ShouldReturnCount() {
    var ns = new Namespace();
    ns.Set("a", Value.FromInt(1));
    ns.Set("b", Value.FromInt(2));
    ns.Length.Should().Be(2);
  }

  [Fact]
  public void Namespace_Call_ShouldReturnNull() {
    var ns = new Namespace();
    var state = new State(new Environment());
    ns.Call([], [], state).Should().BeNull();
  }

  [Fact]
  public void Dict_ShouldCreateDictionary() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set d = dict(a=1, b=2) %}{{ d.a }}-{{ d.b }}");
    var result = tmpl.Render();
    result.Should().Be("1-2");
  }

  [Fact]
  public void Debug_ShouldOutputContext() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ debug() }}");
    var result = tmpl.Render(new Dictionary<string, object?> { ["myvar"] = 42 });
    result.Should().Contain("myvar");
    result.Should().Contain("42");
  }
}
