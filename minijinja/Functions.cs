namespace MiniJinja;

/// <summary>
/// Built-in functions for the template engine.
/// </summary>
public static class BuiltinFunctions {
  public static readonly Dictionary<string, Func<List<Value>, Dictionary<string, Value>, State, Value>> Functions = new() {
    ["range"] = (args, kwargs, _) => {
      long start = 0, stop = 0, step = 1;

      if (args.Count == 1) {
        stop = args[0].AsInt();
      } else if (args.Count == 2) {
        start = args[0].AsInt();
        stop = args[1].AsInt();
      } else if (args.Count >= 3) {
        start = args[0].AsInt();
        stop = args[1].AsInt();
        step = args[2].AsInt();
      }

      if (step == 0) {
        throw new TemplateError("range() step cannot be zero");
      }

      var result = new List<Value>();
      if (step > 0) {
        for (long i = start; i < stop; i += step) {
          result.Add(Value.FromInt(i));
        }
      } else {
        for (long i = start; i > stop; i += step) {
          result.Add(Value.FromInt(i));
        }
      }

      return Value.FromSeq(result);
    },
    ["lipsum"] = (args, kwargs, _) => {
      var n = args.Count > 0 ? (int)args[0].AsInt() : 5;
      var html = true;
      if (kwargs.TryGetValue("html", out var h)) {
        html = h.IsTrue;
      }

      var paragraphs = new List<string>(n);
      var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

      for (var i = 0; i < n; i++) {
        paragraphs.Add(lorem);
      }

      if (html) {
        return Value.FromSafeString(string.Join("\n", paragraphs.Select(p => $"<p>{p}</p>")));
      } else {
        return Value.FromString(string.Join("\n\n", paragraphs));
      }
    },
    ["cycler"] = (args, _, _) => {
      return Value.FromObject(new Cycler(args));
    },
    ["joiner"] = (args, _, _) => {
      var sep = args.Count > 0 ? args[0].AsString() : ", ";
      return Value.FromObject(new Joiner(sep));
    },
    ["namespace"] = (args, kwargs, _) => {
      var ns = new Namespace();
      foreach (var (key, value) in kwargs) {
        ns.Set(key, value);
      }
      return Value.FromObject(ns);
    },
    ["dict"] = (args, kwargs, _) => {
      var dict = new Dictionary<string, Value>(kwargs);
      return Value.FromMap(dict);
    },
    ["debug"] = (_, _, state) => {
      var result = new System.Text.StringBuilder();
      result.AppendLine("Context:");
      foreach (var (key, value) in state.GetAllVariables()) {
        result.AppendLine($"  {key}: {value.ToJson(false)}");
      }
      return Value.FromString(result.ToString());
    },
  };
}

/// <summary>
/// Cycler helper class.
/// </summary>
public class Cycler(List<Value> items) : IObject {
  private int index = 0;

  public Value Current => items.Count > 0 ? items[index] : Value.FromNone();

  public Value Next() {
    if (items.Count == 0) {
      return Value.FromNone();
    }

    var value = items[index];
    index = (index + 1) % items.Count;
    return value;
  }

  public void Reset() {
    index = 0;
  }

  public bool TryGetAttr(string name, out Value value) {
    switch (name) {
      case "current":
        value = Current;
        return true;
      case "next":
        value = Value.FromCallable((args, _, _) => Next());
        return true;
      case "reset":
        value = Value.FromCallable((args, _, _) => { Reset(); return Value.FromNone(); });
        return true;
      default:
        value = Value.FromNone();
        return false;
    }
  }

  public bool TryGetItem(Value key, out Value value) {
    value = Value.FromNone();
    return false;
  }

  public IEnumerable<Value>? TryIter() => null;
  public int? Length => null;
  public Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state) => Next();
}

/// <summary>
/// Joiner helper class.
/// </summary>
public class Joiner(string sep) : IObject {
  private bool used = false;

  public bool TryGetAttr(string name, out Value value) {
    value = Value.FromNone();
    return false;
  }

  public bool TryGetItem(Value key, out Value value) {
    value = Value.FromNone();
    return false;
  }

  public IEnumerable<Value>? TryIter() => null;
  public int? Length => null;

  public Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state) {
    if (!used) {
      used = true;
      return Value.FromString("");
    }
    return Value.FromString(sep);
  }
}

/// <summary>
/// Namespace helper class for storing state.
/// </summary>
public class Namespace : IObject {
  private readonly Dictionary<string, Value> data = new();

  public void Set(string key, Value value) {
    data[key] = value;
  }

  public bool TryGetAttr(string name, out Value value) {
    return data.TryGetValue(name, out value);
  }

  public void SetAttr(string name, Value value) {
    data[name] = value;
  }

  public bool TryGetItem(Value key, out Value value) {
    return data.TryGetValue(key.AsString(), out value);
  }

  public IEnumerable<Value>? TryIter() => null;
  public int? Length => data.Count;
  public Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state) => null;
}
