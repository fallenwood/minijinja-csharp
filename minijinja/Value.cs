namespace MiniJinja;

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>
/// The kind of a Value.
/// </summary>
public enum ValueKind {
  Undefined,
  None,
  Bool,
  Number,
  Int = Number,    // Alias for compatibility
  Float = Number,  // Alias for compatibility
  String,
  Seq,
  Map,
  Callable,
  Object
}

/// <summary>
/// Interface for callable objects.
/// </summary>
public interface ICallable {
  Value Call(Value[] args, Dictionary<string, Value> kwargs);
}

/// <summary>
/// Interface for objects with attribute access.
/// </summary>
public interface IObject {
  bool TryGetAttr(string name, out Value value);
  bool TryGetItem(Value key, out Value value);
  IEnumerable<Value>? TryIter();
  int? Length { get; }
  Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state);
}

/// <summary>
/// Interface for mutable objects.
/// </summary>
public interface IMutableObject : IObject {
  void SetAttr(string name, Value value);
}

/// <summary>
/// Represents a dynamically typed value in the template engine.
/// </summary>
public readonly struct Value : IEquatable<Value> {
  private readonly object? _data;
  private readonly ValueKind _kind;
  private readonly bool _isSafe;

  private Value(object? data, ValueKind kind, bool isSafe = false) {
    _data = data;
    _kind = kind;
    _isSafe = isSafe;
  }

  public static Value Undefined => new(null, ValueKind.Undefined);
  public static Value None => new(null, ValueKind.None);
  public static Value True => new(true, ValueKind.Bool);
  public static Value False => new(false, ValueKind.Bool);

  public static Value FromBool(bool value) => new(value, ValueKind.Bool);
  public static Value FromInt(long value) => new(value, ValueKind.Number);
  public static Value FromDouble(double value) => new(value, ValueKind.Number);
  public static Value FromString(string value) => new(value, ValueKind.String);
  public static Value FromSafeString(string value) => new(value, ValueKind.String, true);
  public static Value FromList(List<Value> value) => new(value, ValueKind.Seq);
  public static Value FromDict(Dictionary<string, Value> value) => new(value, ValueKind.Map);
  public static Value FromCallable(ICallable value) => new(value, ValueKind.Callable);
  public static Value FromCallable(Func<List<Value>, Dictionary<string, Value>, State, Value> func) =>
      new(new DelegateCallable(func), ValueKind.Callable);
  public static Value FromObject(IObject value) => new(value, ValueKind.Object);

  // Aliases for compatibility
  public static Value FromNone() => None;
  public static Value FromUndefined() => Undefined;
  public static Value FromInt(int value) => new((long)value, ValueKind.Number);
  public static Value FromFloat(double value) => new(value, ValueKind.Number);
  public static Value FromSeq(List<Value> value) => FromList(value);
  public static Value FromMap(Dictionary<string, Value> value) => FromDict(value);

  public ValueKind Kind => _kind;
  public bool IsSafe => _isSafe;
  public bool IsUndefined => _kind == ValueKind.Undefined;
  public bool IsNone => _kind == ValueKind.None;

  public bool IsTrue {
    get {
      return _kind switch {
        ValueKind.Undefined => false,
        ValueKind.None => false,
        ValueKind.Bool => (bool)_data!,
        ValueKind.Number => _data switch {
          long l => l != 0,
          double d => d != 0 && !double.IsNaN(d),
          _ => true
        },
        ValueKind.String => !string.IsNullOrEmpty((string)_data!),
        ValueKind.Seq => ((List<Value>)_data!).Count > 0,
        ValueKind.Map => ((Dictionary<string, Value>)_data!).Count > 0,
        _ => true
      };
    }
  }

  public bool TryGetBool(out bool value) {
    if (_kind == ValueKind.Bool) {
      value = (bool)_data!;
      return true;
    }
    value = default;
    return false;
  }

  public bool TryGetLong(out long value) {
    if (_kind == ValueKind.Number) {
      if (_data is long l) {
        value = l;
        return true;
      }
      if (_data is double d && d == Math.Truncate(d)) {
        value = (long)d;
        return true;
      }
    }
    value = default;
    return false;
  }

  public bool TryGetDouble(out double value) {
    if (_kind == ValueKind.Number) {
      if (_data is double d) {
        value = d;
        return true;
      }
      if (_data is long l) {
        value = l;
        return true;
      }
    }
    value = default;
    return false;
  }

  public bool TryGetString(out string? value) {
    if (_kind == ValueKind.String) {
      value = (string)_data!;
      return true;
    }
    value = null;
    return false;
  }

  public bool TryGetList(out List<Value>? value) {
    if (_kind == ValueKind.Seq) {
      value = (List<Value>)_data!;
      return true;
    }
    value = null;
    return false;
  }

  public bool TryGetDict(out Dictionary<string, Value>? value) {
    if (_kind == ValueKind.Map) {
      value = (Dictionary<string, Value>)_data!;
      return true;
    }
    value = null;
    return false;
  }

  public bool TryGetCallable(out ICallable? value) {
    if (_kind == ValueKind.Callable && _data is ICallable c) {
      value = c;
      return true;
    }
    value = null;
    return false;
  }

  public bool TryGetObject(out IObject? value) {
    if (_kind == ValueKind.Object && _data is IObject o) {
      value = o;
      return true;
    }
    value = null;
    return false;
  }

  public int? Length {
    get {
      return _kind switch {
        ValueKind.String => ((string)_data!).Length,
        ValueKind.Seq => ((List<Value>)_data!).Count,
        ValueKind.Map => ((Dictionary<string, Value>)_data!).Count,
        _ => null
      };
    }
  }

  public Value GetItem(Value key) {
    if (_kind == ValueKind.Seq && key.TryGetLong(out var idx)) {
      var list = (List<Value>)_data!;
      if (idx < 0) idx += list.Count;
      if (idx >= 0 && idx < list.Count)
        return list[(int)idx];
      return Undefined;
    }
    if (_kind == ValueKind.Map && key.TryGetString(out var strKey)) {
      var dict = (Dictionary<string, Value>)_data!;
      return dict.TryGetValue(strKey!, out var val) ? val : Undefined;
    }
    if (_kind == ValueKind.String && key.TryGetLong(out var charIdx)) {
      var str = (string)_data!;
      if (charIdx < 0) charIdx += str.Length;
      if (charIdx >= 0 && charIdx < str.Length)
        return FromString(str[(int)charIdx].ToString());
      return Undefined;
    }
    return Undefined;
  }

  public Value GetAttr(string name) {
    if (_kind == ValueKind.Map) {
      var dict = (Dictionary<string, Value>)_data!;
      return dict.TryGetValue(name, out var val) ? val : Undefined;
    }
    if (_kind == ValueKind.Object && _data is IObject obj) {
      return obj.TryGetAttr(name, out var val) ? val : Undefined;
    }
    return Undefined;
  }

  public bool TryGetAttr(string name, out Value value) {
    value = GetAttr(name);
    return value.Kind != ValueKind.Undefined;
  }

  // Accessor methods for evaluator compatibility
  public object? RawValue => _data;

  public string AsString() {
    return ToString();
  }

  public long AsInt() {
    if (TryGetLong(out var l)) return l;
    if (TryGetDouble(out var d)) return (long)d;
    if (TryGetString(out var s) && long.TryParse(s, out var parsed)) return parsed;
    return 0;
  }

  public double AsFloat() {
    if (TryGetDouble(out var d)) return d;
    if (TryGetLong(out var l)) return l;
    if (TryGetString(out var s) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
    return 0;
  }

  public List<Value> AsSeq() {
    if (TryGetList(out var list)) return list!;
    if (_kind == ValueKind.Map) {
      return ((Dictionary<string, Value>)_data!).Select(kv => FromString(kv.Key)).ToList();
    }
    return [];
  }

  public Dictionary<string, Value> AsMap() {
    if (TryGetDict(out var dict)) return dict!;
    return [];
  }

  public bool IsCallable() {
    return _kind == ValueKind.Callable || (_kind == ValueKind.Object && _data is IObject);
  }

  public Value Call(List<Value> args, Dictionary<string, Value> kwargs, State state) {
    if (_data is DelegateCallable dc) {
      return dc.Invoke(args, kwargs, state);
    }
    if (_data is ICallable c) {
      return c.Call(args.ToArray(), kwargs);
    }
    if (_data is IObject obj) {
      return obj.Call(args, kwargs, state) ?? Undefined;
    }
    throw new TemplateError("Value is not callable");
  }

  public bool TryIter(out IEnumerable<Value> iter) {
    if (_kind == ValueKind.Object && _data is IObject obj) {
      var result = obj.TryIter();
      if (result != null) {
        iter = result;
        return true;
      }
    }
    iter = Iterate();
    return _kind == ValueKind.Seq || _kind == ValueKind.Map || _kind == ValueKind.String;
  }

  public string ToJson(bool pretty) {
    var options = new JsonSerializerOptions {
      WriteIndented = pretty
    };
    return JsonSerializer.Serialize(ToJsonObject(), options);
  }

  private object? ToJsonObject() {
    return _kind switch {
      ValueKind.Undefined => null,
      ValueKind.None => null,
      ValueKind.Bool => (bool)_data!,
      ValueKind.Number => _data is long l ? l : (double)_data!,
      ValueKind.String => (string)_data!,
      ValueKind.Seq => ((List<Value>)_data!).Select(v => v.ToJsonObject()).ToList(),
      ValueKind.Map => ((Dictionary<string, Value>)_data!).ToDictionary(kv => kv.Key, kv => kv.Value.ToJsonObject()),
      _ => ToString()
    };
  }

  public IEnumerable<Value> Iterate() {
    if (_kind == ValueKind.Seq) {
      return (List<Value>)_data!;
    }
    if (_kind == ValueKind.Map) {
      return ((Dictionary<string, Value>)_data!).Keys.Select(FromString);
    }
    if (_kind == ValueKind.String) {
      return ((string)_data!).Select(c => FromString(c.ToString()));
    }
    return [];
  }

  public override string ToString() {
    return _kind switch {
      ValueKind.Undefined => "",
      ValueKind.None => "none",
      ValueKind.Bool => (bool)_data! ? "true" : "false",
      ValueKind.Number => _data switch {
        long l => l.ToString(CultureInfo.InvariantCulture),
        double d when double.IsPositiveInfinity(d) => "inf",
        double d when double.IsNegativeInfinity(d) => "-inf",
        double d when double.IsNaN(d) => "nan",
        double d when d == Math.Truncate(d) && Math.Abs(d) < 1e15 => d.ToString("0.0", CultureInfo.InvariantCulture),
        double d => d.ToString("G", CultureInfo.InvariantCulture),
        _ => _data?.ToString() ?? ""
      },
      ValueKind.String => (string)_data!,
      ValueKind.Seq => "[" + string.Join(", ", ((List<Value>)_data!).Select(v => v.ToRepr())) + "]",
      ValueKind.Map => "{" + string.Join(", ", ((Dictionary<string, Value>)_data!)
          .OrderBy(kv => kv.Key)
          .Select(kv => $"\"{kv.Key}\": {kv.Value.ToRepr()}")) + "}",
      ValueKind.Callable => "<callable>",
      ValueKind.Object => _data?.ToString() ?? "<object>",
      _ => ""
    };
  }

  public string ToRepr() {
    return _kind switch {
      ValueKind.Undefined => "undefined",
      ValueKind.None => "none",
      ValueKind.Bool => (bool)_data! ? "true" : "false",
      ValueKind.String => $"\"{_data}\"",
      _ => ToString()
    };
  }

  public bool Equals(Value other) {
    if (_kind != other._kind) return false;
    return _kind switch {
      ValueKind.Undefined => true,
      ValueKind.None => true,
      ValueKind.Bool => (bool)_data! == (bool)other._data!,
      ValueKind.Number => CompareNumbers(this, other) == 0,
      ValueKind.String => (string)_data! == (string)other._data!,
      ValueKind.Seq => ((List<Value>)_data!).SequenceEqual((List<Value>)other._data!),
      ValueKind.Map => CompareDicts((Dictionary<string, Value>)_data!, (Dictionary<string, Value>)other._data!),
      _ => ReferenceEquals(_data, other._data)
    };
  }

  private static bool CompareDicts(Dictionary<string, Value> a, Dictionary<string, Value> b) {
    if (a.Count != b.Count) return false;
    foreach (var kv in a) {
      if (!b.TryGetValue(kv.Key, out var val) || !kv.Value.Equals(val))
        return false;
    }
    return true;
  }

  public override bool Equals(object? obj) => obj is Value other && Equals(other);
  public override int GetHashCode() => HashCode.Combine(_kind, _data);

  public static int CompareNumbers(Value a, Value b) {
    if (a.TryGetDouble(out var da) && b.TryGetDouble(out var db)) {
      return da.CompareTo(db);
    }
    return 0;
  }

  public int CompareTo(Value other) {
    if (_kind == ValueKind.Number && other._kind == ValueKind.Number) {
      return CompareNumbers(this, other);
    }
    if (_kind == ValueKind.String && other._kind == ValueKind.String) {
      return string.Compare((string)_data!, (string)other._data!, StringComparison.Ordinal);
    }
    return 0;
  }

  public bool Contains(Value item) {
    if (_kind == ValueKind.String && item.TryGetString(out var needle)) {
      return ((string)_data!).Contains(needle!);
    }
    if (_kind == ValueKind.Seq) {
      return ((List<Value>)_data!).Any(v => v.Equals(item));
    }
    if (_kind == ValueKind.Map && item.TryGetString(out var key)) {
      return ((Dictionary<string, Value>)_data!).ContainsKey(key!);
    }
    return false;
  }

  public static Value FromAny(object? value) {
    if (value is null) return None;
    if (value is Value v) return v;

    return value switch {
      bool b => FromBool(b),
      int i => FromInt(i),
      long l => FromInt(l),
      float f => FromDouble(f),
      double d => FromDouble(d),
      string s => FromString(s),
      ICallable c => FromCallable(c),
      IObject o => FromObject(o),
      IDictionary<string, object?> dict => FromDict(dict.ToDictionary(kv => kv.Key, kv => FromAny(kv.Value))),
      IDictionary<string, Value> vdict => FromDict(new Dictionary<string, Value>(vdict)),
      IEnumerable<Value> vals => FromList(vals.ToList()),
      IEnumerable enumerable when value is not string => FromList(enumerable.Cast<object?>().Select(FromAny).ToList()),
      _ => FromReflection(value)
    };
  }

  private static Value FromReflection(object obj) {
    var type = obj.GetType();
    var dict = new Dictionary<string, Value>();
    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
      try {
        var propValue = prop.GetValue(obj);
        dict[prop.Name] = FromAny(propValue);
      } catch {
        // Skip properties that can't be read
      }
    }
    return FromDict(dict);
  }
}

/// <summary>
/// Callable wrapper for delegate functions.
/// </summary>
internal class DelegateCallable {
  private readonly Func<List<Value>, Dictionary<string, Value>, State, Value> _func;

  public DelegateCallable(Func<List<Value>, Dictionary<string, Value>, State, Value> func) {
    _func = func;
  }

  public Value Invoke(List<Value> args, Dictionary<string, Value> kwargs, State state) {
    return _func(args, kwargs, state);
  }
}
