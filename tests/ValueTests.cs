namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;
using Value = MiniJinja.Value;

public class ValueTests {
  [Fact]
  public void Undefined_ShouldHaveCorrectProperties() {
    var v = Value.Undefined;
    v.Kind.Should().Be(ValueKind.Undefined);
    v.IsUndefined.Should().BeTrue();
    v.IsTrue.Should().BeFalse();
    v.AsString().Should().Be("");
  }

  [Fact]
  public void None_ShouldHaveCorrectProperties() {
    var v = Value.None;
    v.Kind.Should().Be(ValueKind.None);
    v.IsNone.Should().BeTrue();
    v.IsTrue.Should().BeFalse();
    v.AsString().Should().Be("none");
  }

  [Fact]
  public void Bool_ShouldHaveCorrectProperties() {
    var t = Value.True;
    var f = Value.False;
    t.Kind.Should().Be(ValueKind.Bool);
    t.IsTrue.Should().BeTrue();
    f.IsTrue.Should().BeFalse();
    t.AsString().Should().Be("true");
    f.AsString().Should().Be("false");
  }

  [Fact]
  public void TryGetBool_ShouldWork() {
    var t = Value.FromBool(true);
    t.TryGetBool(out var b).Should().BeTrue();
    b.Should().BeTrue();

    var i = Value.FromInt(1);
    i.TryGetBool(out _).Should().BeFalse();
  }

  [Fact]
  public void Int_ShouldHaveCorrectProperties() {
    var v = Value.FromInt(42);
    v.Kind.Should().Be(ValueKind.Number);
    v.IsTrue.Should().BeTrue();
    v.AsInt().Should().Be(42);
    v.AsFloat().Should().Be(42.0);
    v.AsString().Should().Be("42");
  }

  [Fact]
  public void Int_Zero_ShouldBeFalsy() {
    var v = Value.FromInt(0);
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void Float_ShouldHaveCorrectProperties() {
    var v = Value.FromFloat(3.14);
    v.Kind.Should().Be(ValueKind.Number);
    v.IsTrue.Should().BeTrue();
    v.AsFloat().Should().BeApproximately(3.14, 0.001);
    v.AsInt().Should().Be(3);
  }

  [Fact]
  public void Float_Zero_ShouldBeFalsy() {
    var v = Value.FromFloat(0.0);
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void Float_NaN_ShouldBeFalsy() {
    var v = Value.FromFloat(double.NaN);
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void TryGetLong_ShouldWork() {
    var i = Value.FromInt(42);
    i.TryGetLong(out var l).Should().BeTrue();
    l.Should().Be(42);

    var d = Value.FromFloat(42.0);
    d.TryGetLong(out var l2).Should().BeTrue();
    l2.Should().Be(42);

    var d2 = Value.FromFloat(42.5);
    d2.TryGetLong(out _).Should().BeFalse();

    var s = Value.FromString("x");
    s.TryGetLong(out _).Should().BeFalse();
  }

  [Fact]
  public void TryGetDouble_ShouldWork() {
    var d = Value.FromFloat(3.14);
    d.TryGetDouble(out var dbl).Should().BeTrue();
    dbl.Should().BeApproximately(3.14, 0.001);

    var i = Value.FromInt(42);
    i.TryGetDouble(out var dbl2).Should().BeTrue();
    dbl2.Should().Be(42.0);

    var s = Value.FromString("x");
    s.TryGetDouble(out _).Should().BeFalse();
  }

  [Fact]
  public void String_ShouldHaveCorrectProperties() {
    var v = Value.FromString("hello");
    v.Kind.Should().Be(ValueKind.String);
    v.IsTrue.Should().BeTrue();
    v.AsString().Should().Be("hello");
    v.Length.Should().Be(5);
  }

  [Fact]
  public void String_Empty_ShouldBeFalsy() {
    var v = Value.FromString("");
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void TryGetString_ShouldWork() {
    var s = Value.FromString("test");
    s.TryGetString(out var str).Should().BeTrue();
    str.Should().Be("test");

    var i = Value.FromInt(1);
    i.TryGetString(out _).Should().BeFalse();
  }

  [Fact]
  public void SafeString_ShouldHaveIsSafeTrue() {
    var v = Value.FromSafeString("<b>safe</b>");
    v.IsSafe.Should().BeTrue();
    v.AsString().Should().Be("<b>safe</b>");
  }

  [Fact]
  public void Seq_ShouldHaveCorrectProperties() {
    var v = Value.FromSeq([Value.FromInt(1), Value.FromInt(2)]);
    v.Kind.Should().Be(ValueKind.Seq);
    v.IsTrue.Should().BeTrue();
    v.Length.Should().Be(2);
    v.AsSeq().Should().HaveCount(2);
  }

  [Fact]
  public void Seq_Empty_ShouldBeFalsy() {
    var v = Value.FromSeq([]);
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void TryGetList_ShouldWork() {
    var seq = Value.FromSeq([Value.FromInt(1)]);
    seq.TryGetList(out var list).Should().BeTrue();
    list.Should().HaveCount(1);

    var i = Value.FromInt(1);
    i.TryGetList(out _).Should().BeFalse();
  }

  [Fact]
  public void Map_ShouldHaveCorrectProperties() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    v.Kind.Should().Be(ValueKind.Map);
    v.IsTrue.Should().BeTrue();
    v.Length.Should().Be(1);
    v.AsMap().Should().ContainKey("a");
  }

  [Fact]
  public void Map_Empty_ShouldBeFalsy() {
    var v = Value.FromMap([]);
    v.IsTrue.Should().BeFalse();
  }

  [Fact]
  public void TryGetDict_ShouldWork() {
    var m = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    m.TryGetDict(out var dict).Should().BeTrue();
    dict.Should().ContainKey("a");

    var i = Value.FromInt(1);
    i.TryGetDict(out _).Should().BeFalse();
  }

  [Fact]
  public void GetItem_OnSeq_ShouldWork() {
    var v = Value.FromSeq([Value.FromString("a"), Value.FromString("b")]);
    v.GetItem(Value.FromInt(0)).AsString().Should().Be("a");
    v.GetItem(Value.FromInt(1)).AsString().Should().Be("b");
    v.GetItem(Value.FromInt(-1)).AsString().Should().Be("b");
    v.GetItem(Value.FromInt(5)).IsUndefined.Should().BeTrue();
  }

  [Fact]
  public void GetItem_OnMap_ShouldWork() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["key"] = Value.FromInt(42) });
    v.GetItem(Value.FromString("key")).AsInt().Should().Be(42);
    v.GetItem(Value.FromString("missing")).IsUndefined.Should().BeTrue();
  }

  [Fact]
  public void GetItem_OnString_ShouldWork() {
    var v = Value.FromString("abc");
    v.GetItem(Value.FromInt(0)).AsString().Should().Be("a");
    v.GetItem(Value.FromInt(-1)).AsString().Should().Be("c");
    v.GetItem(Value.FromInt(10)).IsUndefined.Should().BeTrue();
  }

  [Fact]
  public void GetAttr_OnMap_ShouldWork() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["name"] = Value.FromString("test") });
    v.GetAttr("name").AsString().Should().Be("test");
    v.GetAttr("missing").IsUndefined.Should().BeTrue();
  }

  [Fact]
  public void TryGetAttr_ShouldWork() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["x"] = Value.FromInt(1) });
    v.TryGetAttr("x", out var val).Should().BeTrue();
    val.AsInt().Should().Be(1);
    v.TryGetAttr("missing", out _).Should().BeFalse();
  }

  [Fact]
  public void AsSeq_OnMap_ShouldReturnKeys() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    v.AsSeq().Should().HaveCount(1);
    v.AsSeq()[0].AsString().Should().Be("a");
  }

  [Fact]
  public void AsSeq_OnOther_ShouldReturnEmpty() {
    var v = Value.FromInt(42);
    v.AsSeq().Should().BeEmpty();
  }

  [Fact]
  public void AsMap_OnOther_ShouldReturnEmpty() {
    var v = Value.FromInt(42);
    v.AsMap().Should().BeEmpty();
  }

  [Fact]
  public void AsInt_FromString_ShouldParse() {
    var v = Value.FromString("42");
    v.AsInt().Should().Be(42);
  }

  [Fact]
  public void AsInt_FromInvalidString_ShouldReturnZero() {
    var v = Value.FromString("abc");
    v.AsInt().Should().Be(0);
  }

  [Fact]
  public void AsFloat_FromString_ShouldParse() {
    var v = Value.FromString("3.14");
    v.AsFloat().Should().BeApproximately(3.14, 0.001);
  }

  [Fact]
  public void AsFloat_FromInvalidString_ShouldReturnZero() {
    var v = Value.FromString("abc");
    v.AsFloat().Should().Be(0);
  }

  [Fact]
  public void IsCallable_ShouldWork() {
    var callable = Value.FromCallable((args, kwargs, state) => Value.FromInt(1));
    callable.IsCallable().Should().BeTrue();

    var i = Value.FromInt(1);
    i.IsCallable().Should().BeFalse();
  }

  [Fact]
  public void Call_OnCallable_ShouldWork() {
    var callable = Value.FromCallable((args, kwargs, state) => Value.FromInt(args[0].AsInt() * 2));
    var env = new Environment();
    var tmpl = env.TemplateFromString("dummy");
    // We need a state, create via render
    var state = new State(env);
    var result = callable.Call([Value.FromInt(5)], [], state);
    result.AsInt().Should().Be(10);
  }

  [Fact]
  public void Call_OnNonCallable_ShouldThrow() {
    var v = Value.FromInt(1);
    var state = new State(new Environment());
    Action act = () => v.Call([], [], state);
    act.Should().Throw<TemplateError>().WithMessage("*not callable*");
  }

  [Fact]
  public void TryIter_OnSeq_ShouldWork() {
    var v = Value.FromSeq([Value.FromInt(1), Value.FromInt(2)]);
    v.TryIter(out var iter).Should().BeTrue();
    iter.Should().HaveCount(2);
  }

  [Fact]
  public void TryIter_OnMap_ShouldWork() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    v.TryIter(out var iter).Should().BeTrue();
    iter.Should().HaveCount(1);
  }

  [Fact]
  public void TryIter_OnString_ShouldWork() {
    var v = Value.FromString("ab");
    v.TryIter(out var iter).Should().BeTrue();
    iter.Should().HaveCount(2);
  }

  [Fact]
  public void TryIter_OnNumber_ShouldReturnFalse() {
    var v = Value.FromInt(42);
    v.TryIter(out _).Should().BeFalse();
  }

  [Fact]
  public void Iterate_OnSeq_ShouldWork() {
    var v = Value.FromSeq([Value.FromInt(1)]);
    v.Iterate().Should().HaveCount(1);
  }

  [Fact]
  public void Iterate_OnMap_ShouldReturnKeys() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["k"] = Value.FromInt(1) });
    v.Iterate().Should().HaveCount(1);
  }

  [Fact]
  public void Iterate_OnString_ShouldReturnChars() {
    var v = Value.FromString("ab");
    v.Iterate().Should().HaveCount(2);
  }

  [Fact]
  public void Iterate_OnOther_ShouldReturnEmpty() {
    var v = Value.FromInt(42);
    v.Iterate().Should().BeEmpty();
  }

  [Fact]
  public void ToJson_ShouldWork() {
    var v = Value.FromMap(new Dictionary<string, Value> {
      ["str"] = Value.FromString("test"),
      ["num"] = Value.FromInt(42),
      ["arr"] = Value.FromSeq([Value.FromInt(1)])
    });
    var json = v.ToJson(false);
    json.Should().Contain("\"str\":\"test\"");
    json.Should().Contain("\"num\":42");
  }

  [Fact]
  public void ToJson_Pretty_ShouldIndent() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) });
    var json = v.ToJson(true);
    json.Should().Contain("\n");
  }

  [Fact]
  public void ToJson_Undefined_ShouldBeNull() {
    var v = Value.Undefined;
    v.ToJson(false).Should().Be("null");
  }

  [Fact]
  public void ToJson_Infinity_ShouldBeNull() {
    var v = Value.FromFloat(double.PositiveInfinity);
    v.ToJson(false).Should().Be("null");
  }

  [Fact]
  public void ToJson_EscapesSpecialChars() {
    var v = Value.FromString("line1\nline2\ttab\"quote\\backslash");
    var json = v.ToJson(false);
    json.Should().Contain("\\n");
    json.Should().Contain("\\t");
    json.Should().Contain("\\\"");
    json.Should().Contain("\\\\");
  }

  [Fact]
  public void ToRepr_ShouldWork() {
    Value.Undefined.ToRepr().Should().Be("undefined");
    Value.None.ToRepr().Should().Be("none");
    Value.True.ToRepr().Should().Be("true");
    Value.FromString("x").ToRepr().Should().Be("\"x\"");
  }

  [Fact]
  public void Equals_ShouldWork() {
    Value.FromInt(1).Equals(Value.FromInt(1)).Should().BeTrue();
    Value.FromInt(1).Equals(Value.FromInt(2)).Should().BeFalse();
    Value.FromString("a").Equals(Value.FromString("a")).Should().BeTrue();
    Value.None.Equals(Value.None).Should().BeTrue();
    Value.Undefined.Equals(Value.Undefined).Should().BeTrue();
    Value.FromSeq([Value.FromInt(1)]).Equals(Value.FromSeq([Value.FromInt(1)])).Should().BeTrue();
    Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) })
      .Equals(Value.FromMap(new Dictionary<string, Value> { ["a"] = Value.FromInt(1) })).Should().BeTrue();
  }

  [Fact]
  public void Equals_DifferentKinds_ShouldBeFalse() {
    Value.FromInt(1).Equals(Value.FromString("1")).Should().BeFalse();
  }

  [Fact]
  public void CompareTo_Numbers_ShouldWork() {
    Value.FromInt(1).CompareTo(Value.FromInt(2)).Should().BeLessThan(0);
    Value.FromInt(2).CompareTo(Value.FromInt(1)).Should().BeGreaterThan(0);
    Value.FromInt(1).CompareTo(Value.FromInt(1)).Should().Be(0);
  }

  [Fact]
  public void CompareTo_Strings_ShouldWork() {
    Value.FromString("a").CompareTo(Value.FromString("b")).Should().BeLessThan(0);
  }

  [Fact]
  public void CompareTo_DifferentTypes_ShouldReturnZero() {
    Value.FromInt(1).CompareTo(Value.FromString("a")).Should().Be(0);
  }

  [Fact]
  public void Contains_String_ShouldWork() {
    var v = Value.FromString("hello world");
    v.Contains(Value.FromString("world")).Should().BeTrue();
    v.Contains(Value.FromString("xyz")).Should().BeFalse();
  }

  [Fact]
  public void Contains_Seq_ShouldWork() {
    var v = Value.FromSeq([Value.FromInt(1), Value.FromInt(2)]);
    v.Contains(Value.FromInt(1)).Should().BeTrue();
    v.Contains(Value.FromInt(3)).Should().BeFalse();
  }

  [Fact]
  public void Contains_Map_ShouldCheckKey() {
    var v = Value.FromMap(new Dictionary<string, Value> { ["key"] = Value.FromInt(1) });
    v.Contains(Value.FromString("key")).Should().BeTrue();
    v.Contains(Value.FromString("missing")).Should().BeFalse();
  }

  [Fact]
  public void FromAny_ShouldConvertTypes() {
    Value.FromAny(null).IsNone.Should().BeTrue();
    Value.FromAny(true).Kind.Should().Be(ValueKind.Bool);
    Value.FromAny(42).Kind.Should().Be(ValueKind.Number);
    Value.FromAny(42L).Kind.Should().Be(ValueKind.Number);
    Value.FromAny(3.14f).Kind.Should().Be(ValueKind.Number);
    Value.FromAny(3.14).Kind.Should().Be(ValueKind.Number);
    Value.FromAny("test").Kind.Should().Be(ValueKind.String);
    Value.FromAny(new List<int> { 1, 2 }).Kind.Should().Be(ValueKind.Seq);
    Value.FromAny(new Dictionary<string, object?> { ["a"] = 1 }).Kind.Should().Be(ValueKind.Map);
  }

  [Fact]
  public void FromAny_Value_ShouldPassThrough() {
    var v = Value.FromInt(42);
    Value.FromAny(v).Should().Be(v);
  }

  [Fact]
  public void FromAny_ITemplateSerializable_ShouldConvert() {
    var obj = new SerializableTest { Val = 10 };
    var v = Value.FromAny(obj);
    v.Kind.Should().Be(ValueKind.Map);
    v.GetAttr("value").AsInt().Should().Be(10);
  }

  [Fact]
  public void FromAny_UnsupportedType_ShouldThrow() {
    Action act = () => Value.FromAny(new object());
    act.Should().Throw<TemplateError>().WithMessage("*Cannot convert*");
  }

  [Fact]
  public void Length_OnNonContainer_ShouldReturnNull() {
    Value.FromInt(42).Length.Should().BeNull();
  }

  [Fact]
  public void TryGetCallable_ShouldWork() {
    // DelegateCallable doesn't implement ICallable, so we test with a custom ICallable
    var c = Value.FromCallable(new TestCallable());
    c.TryGetCallable(out var callable).Should().BeTrue();
    callable.Should().NotBeNull();

    var i = Value.FromInt(1);
    i.TryGetCallable(out _).Should().BeFalse();
  }

  private class TestCallable : ICallable {
    public Value Call(Value[] args, Dictionary<string, Value> kwargs) => Value.None;
  }

  [Fact]
  public void TryGetObject_ShouldWork() {
    var obj = new TestObject();
    var v = Value.FromObject(obj);
    v.TryGetObject(out var o).Should().BeTrue();
    o.Should().Be(obj);

    var i = Value.FromInt(1);
    i.TryGetObject(out _).Should().BeFalse();
  }

  [Fact]
  public void Object_GetAttr_ShouldWork() {
    var obj = new TestObject();
    var v = Value.FromObject(obj);
    v.GetAttr("value").AsInt().Should().Be(42);
    v.GetAttr("missing").IsUndefined.Should().BeTrue();
  }

  [Fact]
  public void ToString_Float_SpecialValues_ShouldWork() {
    Value.FromFloat(double.PositiveInfinity).ToString().Should().Be("inf");
    Value.FromFloat(double.NegativeInfinity).ToString().Should().Be("-inf");
    Value.FromFloat(double.NaN).ToString().Should().Be("nan");
  }

  [Fact]
  public void ToString_Float_WholeNumber_ShouldShowDecimal() {
    Value.FromFloat(5.0).ToString().Should().Be("5.0");
  }

  [Fact]
  public void GetHashCode_ShouldWork() {
    var v1 = Value.FromInt(42);
    var v2 = Value.FromInt(42);
    v1.GetHashCode().Should().Be(v2.GetHashCode());
  }

  [Fact]
  public void Callable_IsTrue_ShouldBeTrue() {
    var c = Value.FromCallable((args, kwargs, state) => Value.None);
    c.IsTrue.Should().BeTrue();
  }

  [Fact]
  public void Object_IsTrue_ShouldBeTrue() {
    var v = Value.FromObject(new TestObject());
    v.IsTrue.Should().BeTrue();
  }

  private class SerializableTest : ITemplateSerializable {
    public int Val { get; set; }
    public Dictionary<string, MiniJinja.Value> ToTemplateValues() =>
      new() { ["value"] = MiniJinja.Value.FromInt(Val) };
  }

  private class TestObject : IObject {
    public bool TryGetAttr(string name, out MiniJinja.Value value) {
      if (name == "value") {
        value = MiniJinja.Value.FromInt(42);
        return true;
      }
      value = MiniJinja.Value.Undefined;
      return false;
    }
    public bool TryGetItem(MiniJinja.Value key, out MiniJinja.Value value) { value = MiniJinja.Value.Undefined; return false; }
    public IEnumerable<MiniJinja.Value>? TryIter() => null;
    public int? Length => null;
    public MiniJinja.Value? Call(List<MiniJinja.Value> args, Dictionary<string, MiniJinja.Value> kwargs, State state) => null;
  }
}
