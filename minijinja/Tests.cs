namespace MiniJinja;

/// <summary>
/// Built-in tests for the template engine.
/// </summary>
public static class BuiltinTests {
  public static readonly Dictionary<string, Func<Value, List<Value>, bool>> Tests = new() {
    ["defined"] = (v, _) => v.Kind != ValueKind.Undefined,
    ["undefined"] = (v, _) => v.Kind == ValueKind.Undefined,
    ["none"] = (v, _) => v.Kind == ValueKind.None,
    ["true"] = (v, _) => v.Kind == ValueKind.Bool && v.IsTrue,
    ["false"] = (v, _) => v.Kind == ValueKind.Bool && !v.IsTrue,
    ["odd"] = (v, _) => v.AsInt() % 2 != 0,
    ["even"] = (v, _) => v.AsInt() % 2 == 0,
    ["divisibleby"] = (v, args) => {
      if (args.Count < 1) throw new TemplateError("divisibleby requires an argument");
      var divisor = args[0].AsInt();
      return divisor != 0 && v.AsInt() % divisor == 0;
    },
    ["number"] = (v, _) => v.Kind == ValueKind.Number || v.Kind == ValueKind.Number,
    ["string"] = (v, _) => v.Kind == ValueKind.String,
    ["sequence"] = (v, _) => v.Kind == ValueKind.Seq,
    ["mapping"] = (v, _) => v.Kind == ValueKind.Map,
    ["iterable"] = (v, _) => v.Kind == ValueKind.Seq || v.Kind == ValueKind.Map || v.Kind == ValueKind.String,
    ["callable"] = (v, _) => v.IsCallable(),
    ["sameas"] = (v, args) => {
      if (args.Count < 1) return false;
      return ReferenceEquals(v.RawValue, args[0].RawValue);
    },
    ["eq"] = (v, args) => args.Count > 0 && v.Equals(args[0]),
    ["equalto"] = (v, args) => args.Count > 0 && v.Equals(args[0]),
    ["=="] = (v, args) => args.Count > 0 && v.Equals(args[0]),
    ["ne"] = (v, args) => args.Count < 1 || !v.Equals(args[0]),
    ["!="] = (v, args) => args.Count < 1 || !v.Equals(args[0]),
    ["lt"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) < 0;
    },
    ["lessthan"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) < 0;
    },
    ["<"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) < 0;
    },
    ["le"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) <= 0;
    },
    ["<="] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) <= 0;
    },
    ["gt"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) > 0;
    },
    ["greaterthan"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) > 0;
    },
    [">"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) > 0;
    },
    ["ge"] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) >= 0;
    },
    [">="] = (v, args) => {
      if (args.Count < 1) return false;
      return CompareValues(v, args[0]) >= 0;
    },
    ["in"] = (v, args) => {
      if (args.Count < 1) return false;
      var container = args[0];
      if (container.Kind == ValueKind.Seq) {
        return container.AsSeq().Any(x => x.Equals(v));
      }
      if (container.Kind == ValueKind.String) {
        return container.AsString().Contains(v.AsString());
      }
      if (container.Kind == ValueKind.Map) {
        return container.AsMap().ContainsKey(v.AsString());
      }
      return false;
    },
    ["lower"] = (v, _) => {
      var s = v.AsString();
      return s.Length > 0 && s == s.ToLowerInvariant();
    },
    ["upper"] = (v, _) => {
      var s = v.AsString();
      return s.Length > 0 && s == s.ToUpperInvariant();
    },
    ["startingwith"] = (v, args) => {
      if (args.Count < 1) return false;
      return v.AsString().StartsWith(args[0].AsString());
    },
    ["endingwith"] = (v, args) => {
      if (args.Count < 1) return false;
      return v.AsString().EndsWith(args[0].AsString());
    },
    ["truthy"] = (v, _) => v.IsTrue,
    ["falsy"] = (v, _) => !v.IsTrue,
  };

  public static bool RunTest(Value value, string testName, List<Value> args, State state) {
    if (Tests.TryGetValue(testName, out var test)) {
      return test(value, args);
    }

    // Check custom tests
    if (state.Environment.HasTest(testName)) {
      return state.Environment.RunTest(testName, value, args);
    }

    throw new TemplateError($"Unknown test: {testName}");
  }

  private static int CompareValues(Value a, Value b) {
    if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
      return a.AsInt().CompareTo(b.AsInt());
    if ((a.Kind == ValueKind.Number || a.Kind == ValueKind.Number) &&
        (b.Kind == ValueKind.Number || b.Kind == ValueKind.Number))
      return a.AsFloat().CompareTo(b.AsFloat());
    return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal);
  }
}
