namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;
using Value = MiniJinja.Value;

public class BuiltinTestsTests {
  [Fact]
  public void NoneTest_ShouldWork() {
    // Parser doesn't support 'is none' as none is a keyword
    // Test the builtin test directly
    var test = BuiltinTests.Tests["none"];
    test(Value.None, []).Should().BeTrue();
    test(Value.FromInt(1), []).Should().BeFalse();
  }

  [Fact]
  public void None_WhenNotNone_ShouldReturnFalse() {
    var test = BuiltinTests.Tests["none"];
    test(Value.FromInt(1), []).Should().BeFalse();
  }

  [Fact]
  public void TrueTest_ShouldWork() {
    // The 'true' and 'false' tests check if a value is exactly true/false bool
    // Since the parser doesn't support 'is true' or 'is false' as keywords,
    // we test through the builtin tests directly
    var trueTest = BuiltinTests.Tests["true"];
    var falseTest = BuiltinTests.Tests["false"];

    trueTest(Value.FromBool(true), []).Should().BeTrue();
    trueTest(Value.FromBool(false), []).Should().BeFalse();
    falseTest(Value.FromBool(false), []).Should().BeTrue();
    falseTest(Value.FromBool(true), []).Should().BeFalse();
  }

  [Theory]
  [InlineData("{{ 5 is odd }}", "true")]
  [InlineData("{{ 4 is odd }}", "false")]
  [InlineData("{{ 4 is even }}", "true")]
  [InlineData("{{ 5 is even }}", "false")]
  [InlineData("{{ 10 is divisibleby(2) }}", "true")]
  [InlineData("{{ 10 is divisibleby(3) }}", "false")]
  [InlineData("{{ 0 is divisibleby(0) }}", "false")]
  public void NumericTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 42 is number }}", "true")]
  [InlineData("{{ 'hello' is number }}", "false")]
  [InlineData("{{ 'hello' is string }}", "true")]
  [InlineData("{{ 42 is string }}", "false")]
  [InlineData("{{ [1,2] is sequence }}", "true")]
  [InlineData("{{ 'x' is sequence }}", "false")]
  [InlineData("{{ {} is mapping }}", "true")]
  [InlineData("{{ [1] is mapping }}", "false")]
  public void TypeTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ [1,2] is iterable }}", "true")]
  [InlineData("{{ {} is iterable }}", "true")]
  [InlineData("{{ 'abc' is iterable }}", "true")]
  [InlineData("{{ 42 is iterable }}", "false")]
  public void IterableTest_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 5 is eq(5) }}", "true")]
  [InlineData("{{ 5 is eq(4) }}", "false")]
  [InlineData("{{ 5 is equalto(5) }}", "true")]
  [InlineData("{{ 5 is ne(4) }}", "true")]
  [InlineData("{{ 5 is ne(5) }}", "false")]
  public void EqualityTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 3 is lt(5) }}", "true")]
  [InlineData("{{ 5 is lt(3) }}", "false")]
  [InlineData("{{ 3 is lessthan(5) }}", "true")]
  [InlineData("{{ 3 is le(3) }}", "true")]
  [InlineData("{{ 3 is le(2) }}", "false")]
  [InlineData("{{ 5 is gt(3) }}", "true")]
  [InlineData("{{ 3 is gt(5) }}", "false")]
  [InlineData("{{ 5 is greaterthan(3) }}", "true")]
  [InlineData("{{ 5 is ge(5) }}", "true")]
  [InlineData("{{ 5 is ge(6) }}", "false")]
  public void ComparisonTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void InTest_List_ShouldWork() {
    var test = BuiltinTests.Tests["in"];
    test(Value.FromInt(2), [Value.FromSeq([Value.FromInt(1), Value.FromInt(2)])]).Should().BeTrue();
    test(Value.FromInt(5), [Value.FromSeq([Value.FromInt(1), Value.FromInt(2)])]).Should().BeFalse();
  }

  [Fact]
  public void InTest_String_ShouldWork() {
    var test = BuiltinTests.Tests["in"];
    test(Value.FromString("el"), [Value.FromString("hello")]).Should().BeTrue();
    test(Value.FromString("x"), [Value.FromString("hello")]).Should().BeFalse();
  }

  [Theory]
  [InlineData("{{ 'abc' is lower }}", "true")]
  [InlineData("{{ 'ABC' is lower }}", "false")]
  [InlineData("{{ '' is lower }}", "false")]
  [InlineData("{{ 'ABC' is upper }}", "true")]
  [InlineData("{{ 'abc' is upper }}", "false")]
  [InlineData("{{ '' is upper }}", "false")]
  public void CaseTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'hello' is startingwith('he') }}", "true")]
  [InlineData("{{ 'hello' is startingwith('lo') }}", "false")]
  [InlineData("{{ 'hello' is endingwith('lo') }}", "true")]
  [InlineData("{{ 'hello' is endingwith('he') }}", "false")]
  public void StringTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 1 is truthy }}", "true")]
  [InlineData("{{ 0 is truthy }}", "false")]
  [InlineData("{{ 'x' is truthy }}", "true")]
  [InlineData("{{ '' is truthy }}", "false")]
  [InlineData("{{ 0 is falsy }}", "true")]
  [InlineData("{{ 1 is falsy }}", "false")]
  public void TruthyFalsyTests_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void SameAs_ShouldCheckReferenceEquality() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x is sameas(y) }}");
    // For primitives, sameas checks reference equality which will be false for boxed values
    var result = tmpl.Render(new Dictionary<string, object?> { ["x"] = 1, ["y"] = 1 });
    // Since values are boxed separately, sameas should be false
    result.Should().Be("false");
  }

  [Fact]
  public void InTest_WithMap_ShouldCheckKey() {
    var test = BuiltinTests.Tests["in"];
    var map = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    test(Value.FromString("a"), [map]).Should().BeTrue();
    test(Value.FromString("b"), [map]).Should().BeFalse();
  }

  [Fact]
  public void CustomTest_ShouldWork() {
    var env = new Environment();
    env.AddTest("positive", v => v.AsInt() > 0);
    var tmpl = env.TemplateFromString("{{ 5 is positive }}");
    var result = tmpl.Render();
    result.Should().Be("true");
  }

  [Fact]
  public void CustomTestWithArgs_ShouldWork() {
    var env = new Environment();
    env.AddTest("between", (v, args) => {
      var val = v.AsInt();
      var min = args[0].AsInt();
      var max = args[1].AsInt();
      return val >= min && val <= max;
    });
    var tmpl = env.TemplateFromString("{{ 5 is between(1, 10) }}");
    var result = tmpl.Render();
    result.Should().Be("true");
  }

  [Fact]
  public void Callable_ShouldDetectCallable() {
    var env = new Environment();
    env.AddFunction("myfunc", () => Value.FromInt(42));
    var tmpl = env.TemplateFromString("{{ myfunc is callable }}");
    var result = tmpl.Render();
    result.Should().Be("true");
  }

  [Fact]
  public void DivisibleByRequiresArg_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 10 is divisibleby }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*requires an argument*");
  }

  [Fact]
  public void UnknownTest_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ x is unknowntest }}");
    Action act = () => tmpl.Render(new Dictionary<string, object?> { ["x"] = 1 });
    act.Should().Throw<TemplateError>().WithMessage("*Unknown test*");
  }

  [Theory]
  [InlineData("{{ 5 is lt }}", "false")]
  [InlineData("{{ 5 is le }}", "false")]
  [InlineData("{{ 5 is gt }}", "false")]
  [InlineData("{{ 5 is ge }}", "false")]
  [InlineData("{{ 5 is eq }}", "false")]
  [InlineData("{{ 'x' is startingwith }}", "false")]
  [InlineData("{{ 'x' is endingwith }}", "false")]
  [InlineData("{{ 'x' is sameas }}", "false")]
  public void TestsWithMissingArgs_ShouldReturnFalse(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void InTest_NoArgs_ShouldReturnFalse() {
    var test = BuiltinTests.Tests["in"];
    test(Value.FromString("x"), []).Should().BeFalse();
  }
}
