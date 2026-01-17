namespace MiniJinja;

using System.Collections.Frozen;
using System.Text;
using System.Web;

/// <summary>
/// Built-in filters for the template engine.
/// </summary>
public static class BuiltinFilters {
  public static readonly FrozenDictionary<string, Func<Value, List<Value>, Dictionary<string, Value>, State, Value>> Filters = (new Dictionary<string, Func<Value, List<Value>, Dictionary<string, Value>, State, Value>>() {
    ["upper"] = (v, _, _, _) => Value.FromString(v.AsString().ToUpperInvariant()),
    ["lower"] = (v, _, _, _) => Value.FromString(v.AsString().ToLowerInvariant()),
    ["capitalize"] = (v, _, _, _) => {
      var s = v.AsString();
      if (string.IsNullOrEmpty(s)) {
        return Value.FromString(s);
      }

      return Value.FromString(char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant());
    },
    ["title"] = (v, _, _, _) => {
      var s = v.AsString();
      var sb = new StringBuilder();
      var newWord = true;
      foreach (var c in s) {
        if (char.IsWhiteSpace(c)) {
          newWord = true;
          sb.Append(c);
        } else if (newWord) {
          sb.Append(char.ToUpperInvariant(c));
          newWord = false;
        } else {
          sb.Append(char.ToLowerInvariant(c));
        }
      }

      return Value.FromString(sb.ToString());
    },
    ["trim"] = (v, _, _, _) => Value.FromString(v.AsString().Trim()),
    ["length"] = (v, _, _, _) => {
      if (v.Kind == ValueKind.String) return Value.FromInt(v.AsString().Length);
      if (v.Kind == ValueKind.Seq) return Value.FromInt(v.AsSeq().Count);
      if (v.Kind == ValueKind.Map) return Value.FromInt(v.AsMap().Count);
      return Value.FromInt(0);
    },
    ["first"] = (v, _, _, _) => {
      var seq = v.AsSeq();
      return seq.Count > 0 ? seq[0] : Value.FromNone();
    },
    ["last"] = (v, _, _, _) => {
      var seq = v.AsSeq();
      return seq.Count > 0 ? seq[^1] : Value.FromNone();
    },
    ["reverse"] = (v, _, _, _) => {
      if (v.Kind == ValueKind.String) {
        var chars = v.AsString().ToCharArray();
        Array.Reverse(chars);
        return Value.FromString(new string(chars));
      }
      var seq = v.AsSeq().ToList();
      seq.Reverse();
      return Value.FromSeq(seq);
    },
    ["sort"] = (v, args, kwargs, _) => {
      var seq = v.AsSeq().ToList();
      var reverse = false;
      string? attr = null;

      if (kwargs.TryGetValue("reverse", out var rev)) {
        reverse = rev.IsTrue;
      }

      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      if (attr != null) {
        seq.Sort((a, b) => {
          var aVal = GetAttr(a, attr);
          var bVal = GetAttr(b, attr);
          return CompareValues(aVal, bVal);
        });
      } else {
        seq.Sort(CompareValues);
      }

      if (reverse) {
        seq.Reverse();
      }

      return Value.FromSeq(seq);
    },
    ["join"] = (v, args, _, _) => {
      var sep = args.Count > 0 ? args[0].AsString() : "";
      var seq = v.AsSeq();
      return Value.FromString(string.Join(sep, seq.Select(x => x.AsString())));
    },
    ["replace"] = (v, args, _, _) => {
      if (args.Count < 2) {
        throw new TemplateError("replace requires 2 arguments");
      }

      var old = args[0].AsString();
      var newStr = args[1].AsString();
      return Value.FromString(v.AsString().Replace(old, newStr));
    },
    ["split"] = (v, args, _, _) => {
      var sep = args.Count > 0 ? args[0].AsString() : " ";
      var parts = v.AsString().Split(sep);
      return Value.FromSeq(parts.Select(Value.FromString).ToList());
    },
    ["abs"] = (v, _, _, _) => {
      if (v.Kind == ValueKind.Number) {
        if (v.TryGetLong(out var l)) {
          return Value.FromInt(Math.Abs(l));
        }

        if (v.TryGetDouble(out var d)) {
          return Value.FromFloat(Math.Abs(d));
        }
      }
      return v;
    },
    ["int"] = (v, args, _, _) => {
      var def = args.Count > 0 ? args[0].AsInt() : 0;
      try {
        if (v.Kind == ValueKind.Number) {
          // Always truncate to integer
          return Value.FromInt(v.AsInt());
        }
        if (v.Kind == ValueKind.String && long.TryParse(v.AsString(), out var i)) {
          return Value.FromInt(i);
        }

        return Value.FromInt(def);
      } catch {
        return Value.FromInt(def);
      }
    },
    ["float"] = (v, args, _, _) => {
      var def = args.Count > 0 ? args[0].AsFloat() : 0.0;
      try {
        if (v.Kind == ValueKind.Number) {
          return Value.FromFloat(v.AsFloat());
        }
        if (v.Kind == ValueKind.String && double.TryParse(v.AsString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var f)) {
          return Value.FromFloat(f);
        }

        return Value.FromFloat(def);
      } catch {
        return Value.FromFloat(def);
      }
    },
    ["string"] = (v, _, _, _) => Value.FromString(v.AsString()),
    ["default"] = (v, args, kwargs, _) => {
      var def = args.Count > 0 ? args[0] : Value.FromString("");
      var checkBoolean = false;
      if (kwargs.TryGetValue("boolean", out var b)) {
        checkBoolean = b.IsTrue;
      }

      if (v.Kind == ValueKind.Undefined) {
        return def;
      }

      if (checkBoolean && !v.IsTrue) {
        return def;
      }

      return v;
    },
    ["d"] = (v, args, kwargs, state) => Filters!["default"](v, args, kwargs, state),
    ["list"] = (v, _, _, _) => {
      if (v.Kind == ValueKind.String) {
        return Value.FromSeq(v.AsString().Select(c => Value.FromString(c.ToString())).ToList());
      }
      return Value.FromSeq(v.AsSeq().ToList());
    },
    ["batch"] = (v, args, _, _) => {
      if (args.Count < 1) {
        throw new TemplateError("batch requires count argument");
      }

      var count = (int)args[0].AsInt();
      var fill = args.Count > 1 ? args[1] : Value.FromNone();

      var seq = v.AsSeq().ToList();
      var result = new List<Value>();

      for (var i = 0; i < seq.Count; i += count) {
        var batch = new List<Value>();
        for (var j = 0; j < count; j++) {
          if (i + j < seq.Count) {
            batch.Add(seq[i + j]);
          } else if (fill.Kind != ValueKind.None) {
            batch.Add(fill);
          }
        }
        result.Add(Value.FromSeq(batch));
      }

      return Value.FromSeq(result);
    },
    ["slice"] = (v, args, _, _) => {
      if (args.Count < 1) {
        throw new TemplateError("slice requires slices argument");
      }

      var slices = (int)args[0].AsInt();
      var fill = args.Count > 1 ? args[1] : Value.FromNone();

      var seq = v.AsSeq().ToList();
      var result = new List<Value>();
      var sliceSize = (seq.Count + slices - 1) / slices;

      for (var i = 0; i < slices; i++) {
        var sliceItems = new List<Value>();
        for (var j = 0; j < sliceSize; j++) {
          var idx = i * sliceSize + j;
          if (idx < seq.Count) {
            sliceItems.Add(seq[idx]);
          } else if (fill.Kind != ValueKind.None) {
            sliceItems.Add(fill);
          }
        }
        if (sliceItems.Count > 0) {
          result.Add(Value.FromSeq(sliceItems));
        }
      }

      return Value.FromSeq(result);
    },
    ["items"] = (v, _, _, _) => {
      var map = v.AsMap();
      var items = map.Select(kv => Value.FromSeq([Value.FromString(kv.Key), kv.Value])).ToList();
      return Value.FromSeq(items);
    },
    ["dictsort"] = (v, args, kwargs, _) => {
      var map = v.AsMap();
      var byValue = false;
      var reverse = false;
      var caseSensitive = true;

      if (kwargs.TryGetValue("by", out var by)) {
        byValue = by.AsString() == "value";
      }

      if (kwargs.TryGetValue("reverse", out var rev)) {
        reverse = rev.IsTrue;
      }

      if (kwargs.TryGetValue("case_sensitive", out var cs)) {
        caseSensitive = cs.IsTrue;
      }

      var items = map.Select(kv => (kv.Key, kv.Value)).ToList();

      items.Sort((a, b) => {
        var aVal = byValue ? a.Value.AsString() : a.Key;
        var bVal = byValue ? b.Value.AsString() : b.Key;
        if (!caseSensitive) {
          aVal = aVal.ToLowerInvariant();
          bVal = bVal.ToLowerInvariant();
        }
        return string.Compare(aVal, bVal, StringComparison.Ordinal);
      });

      if (reverse) {
        items.Reverse();
      }

      return Value.FromSeq([.. items.Select(kv => Value.FromSeq([Value.FromString(kv.Key), kv.Value]))]);
    },
    ["groupby"] = (v, args, kwargs, _) => {
      string? attr = null;
      if (args.Count > 0) {
        attr = args[0].AsString();
      }

      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      if (attr == null) {
        throw new TemplateError("groupby requires attribute");
      }

      var seq = v.AsSeq();
      var groups = new Dictionary<string, List<Value>>();
      var order = new List<string>();

      foreach (var item in seq) {
        var key = GetAttr(item, attr).AsString();
        if (!groups.ContainsKey(key)) {
          groups[key] = [];
          order.Add(key);
        }

        groups[key].Add(item);
      }

      var result = order.Select(k => {
        var group = new Dictionary<string, Value> {
          ["grouper"] = Value.FromString(k),
          ["list"] = Value.FromSeq(groups[k])
        };
        return Value.FromMap(group);
      }).ToList();

      return Value.FromSeq(result);
    },
    ["map"] = (v, args, kwargs, state) => {
      string? attr = null;
      string? filter = null;

      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      if (args.Count > 0 && attr == null) {
        filter = args[0].AsString();
      }

      var seq = v.AsSeq();
      var result = new List<Value>();

      foreach (var item in seq) {
        if (attr != null) {
          result.Add(GetAttr(item, attr));
        } else if (filter != null && Filters!.TryGetValue(filter, out var filterFunc)) {
          var filterArgs = args.Skip(1).ToList();
          result.Add(filterFunc(item, filterArgs, [], state));
        } else {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["select"] = (v, args, kwargs, state) => {
      var testName = args.Count > 0 ? args[0].AsString() : "truthy";
      var seq = v.AsSeq();
      var result = new List<Value>();

      foreach (var item in seq) {
        if (BuiltinTests.RunTest(item, testName, args.Skip(1).ToList(), state)) {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["reject"] = (v, args, kwargs, state) => {
      var testName = args.Count > 0 ? args[0].AsString() : "truthy";
      var seq = v.AsSeq();
      var result = new List<Value>();

      foreach (var item in seq) {
        if (!BuiltinTests.RunTest(item, testName, args.Skip(1).ToList(), state)) {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["selectattr"] = (v, args, kwargs, state) => {
      if (args.Count < 1) {
        throw new TemplateError("selectattr requires attribute name");
      }

      var attr = args[0].AsString();
      var testName = args.Count > 1 ? args[1].AsString() : "truthy";
      var seq = v.AsSeq();
      var result = new List<Value>();

      foreach (var item in seq) {
        var attrValue = GetAttr(item, attr);
        if (BuiltinTests.RunTest(attrValue, testName, args.Skip(2).ToList(), state)) {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["rejectattr"] = (v, args, kwargs, state) => {
      if (args.Count < 1) {
        throw new TemplateError("rejectattr requires attribute name");
      }

      var attr = args[0].AsString();
      var testName = args.Count > 1 ? args[1].AsString() : "truthy";
      var seq = v.AsSeq();
      var result = new List<Value>();

      foreach (var item in seq) {
        var attrValue = GetAttr(item, attr);
        if (!BuiltinTests.RunTest(attrValue, testName, args.Skip(2).ToList(), state)) {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["unique"] = (v, args, kwargs, _) => {
      var seq = v.AsSeq();
      var seen = new HashSet<string>();
      var result = new List<Value>();

      string? attr = null;
      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      foreach (var item in seq) {
        var key = attr != null ? GetAttr(item, attr).AsString() : item.AsString();
        if (seen.Add(key)) {
          result.Add(item);
        }
      }

      return Value.FromSeq(result);
    },
    ["min"] = (v, args, kwargs, _) => {
      var seq = v.AsSeq().ToList();
      if (seq.Count == 0) {
        return Value.FromNone();
      }

      string? attr = null;
      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      return seq.MinBy(x => attr != null ? GetAttr(x, attr) : x, Comparer<Value>.Create(CompareValues))!;
    },
    ["max"] = (v, args, kwargs, _) => {
      var seq = v.AsSeq().ToList();
      if (seq.Count == 0) {
        return Value.FromNone();
      }

      string? attr = null;
      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      return seq.MaxBy(x => attr != null ? GetAttr(x, attr) : x, Comparer<Value>.Create(CompareValues))!;
    },
    ["sum"] = (v, args, kwargs, _) => {
      var seq = v.AsSeq();
      var start = args.Count > 0 ? args[0].AsFloat() : 0;

      string? attr = null;
      if (kwargs.TryGetValue("attribute", out var attrVal)) {
        attr = attrVal.AsString();
      }

      foreach (var item in seq) {
        var val = attr != null ? GetAttr(item, attr) : item;
        start += val.AsFloat();
      }

      if (start == Math.Floor(start)) {
        return Value.FromInt((long)start);
      }

      return Value.FromFloat(start);
    },
    ["round"] = (v, args, kwargs, _) => {
      var precision = args.Count > 0 ? (int)args[0].AsInt() : 0;
      var method = "common";
      if (kwargs.TryGetValue("method", out var m)) {
        method = m.AsString();
      }

      var val = v.AsFloat();
      var result = method switch {
        "ceil" => Math.Ceiling(val * Math.Pow(10, precision)) / Math.Pow(10, precision),
        "floor" => Math.Floor(val * Math.Pow(10, precision)) / Math.Pow(10, precision),
        _ => Math.Round(val, precision, MidpointRounding.AwayFromZero)
      };

      if (precision == 0) {
        return Value.FromInt((long)result);
      }

      return Value.FromFloat(result);
    },
    ["attr"] = (v, args, _, _) => {
      if (args.Count < 1) {
        throw new TemplateError("attr requires attribute name");
      }

      return GetAttr(v, args[0].AsString());
    },
    ["tojson"] = (v, args, kwargs, _) => {
      var pretty = false;
      if (kwargs.TryGetValue("indent", out var indent)) {
        pretty = indent.AsInt() > 0;
      }

      return Value.FromSafeString(v.ToJson(pretty));
    },
    ["safe"] = (v, _, _, _) => Value.FromSafeString(v.AsString()),
    ["escape"] = (v, _, _, _) => Value.FromString(HttpUtility.HtmlEncode(v.AsString())),
    ["e"] = (v, args, kwargs, state) => Filters!["escape"](v, args, kwargs, state),
    ["striptags"] = (v, _, _, _) => {
      var s = v.AsString();
      // TODO: Regex
      var result = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]*>", "");
      return Value.FromString(result);
    },
    ["urlencode"] = (v, _, _, _) => Value.FromString(Uri.EscapeDataString(v.AsString())),
    ["indent"] = (v, args, kwargs, _) => {
      var width = args.Count > 0 ? (int)args[0].AsInt() : 4;
      var first = false;
      var blank = false;

      if (kwargs.TryGetValue("first", out var f)) {
        first = f.IsTrue;
      }

      if (kwargs.TryGetValue("blank", out var b)) {
        blank = b.IsTrue;
      }

      var indent = new string(' ', width);
      var lines = v.AsString().Split('\n');
      var result = new StringBuilder();

      for (var i = 0; i < lines.Length; i++) {
        var line = lines[i];
        if (i > 0) {
          result.Append('\n');
        }

        if ((i == 0 && !first) || (string.IsNullOrWhiteSpace(line) && !blank)) {
          result.Append(line);
        } else {
          result.Append(indent);
          result.Append(line);
        }
      }

      return Value.FromString(result.ToString());
    },
    ["wordcount"] = (v, _, _, _) => {
      var words = v.AsString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
      return Value.FromInt(words.Length);
    },
    ["truncate"] = (v, args, kwargs, _) => {
      var length = args.Count > 0 ? (int)args[0].AsInt() : 255;
      var end = "...";
      var killwords = false;
      var leeway = 0;

      if (kwargs.TryGetValue("end", out var e)) {
        end = e.AsString();
      }

      if (kwargs.TryGetValue("killwords", out var k)) {
        killwords = k.IsTrue;
      }

      if (kwargs.TryGetValue("leeway", out var l)) {
        leeway = (int)l.AsInt();
      }

      var s = v.AsString();
      if (s.Length <= length + leeway) {
        return Value.FromString(s);
      }

      if (killwords) {
        return Value.FromString(s[..(length - end.Length)] + end);
      } else {
        var truncated = s[..(length - end.Length)];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 0) {
          truncated = truncated[..lastSpace];
        }

        return Value.FromString(truncated + end);
      }
    },
    ["wordwrap"] = (v, args, kwargs, _) => {
      var width = args.Count > 0 ? (int)args[0].AsInt() : 79;
      var breakLongWords = true;
      var breakOnHyphens = true;
      var wrapString = "\n";

      if (kwargs.TryGetValue("break_long_words", out var blw)) {
        breakLongWords = blw.IsTrue;
      }

      if (kwargs.TryGetValue("break_on_hyphens", out var boh)) {
        breakOnHyphens = boh.IsTrue;
      }

      if (kwargs.TryGetValue("wrapstring", out var ws)) {
        wrapString = ws.AsString();
      }

      var words = v.AsString().Split(' ');
      var result = new StringBuilder();
      var lineLength = 0;

      foreach (var word in words) {
        if (lineLength + word.Length + 1 > width && lineLength > 0) {
          result.Append(wrapString);
          lineLength = 0;
        }

        if (lineLength > 0) {
          result.Append(' ');
          lineLength++;
        }

        result.Append(word);
        lineLength += word.Length;
      }

      return Value.FromString(result.ToString());
    },
    ["center"] = (v, args, _, _) => {
      var width = args.Count > 0 ? (int)args[0].AsInt() : 80;
      var s = v.AsString();
      if (s.Length >= width) {
        return Value.FromString(s);
      }

      var padding = width - s.Length;
      var left = padding / 2;
      var right = padding - left;
      return Value.FromString(new string(' ', left) + s + new string(' ', right));
    },
    ["format"] = (v, args, kwargs, _) => {
      var format = v.AsString();
      // Simple positional replacement
      var result = format;
      for (var i = 0; i < args.Count; i++) {
        result = result.Replace($"%s", args[i].AsString(), StringComparison.Ordinal);
      }

      foreach (var (key, value) in kwargs) {
        result = result.Replace($"%({key})s", value.AsString());
      }

      return Value.FromString(result);
    },
    ["pprint"] = (v, _, _, _) => Value.FromString(v.ToJson(true)),
    ["xmlattr"] = (v, args, _, _) => {
      var map = v.AsMap();
      var autospace = args.Count > 0 ? args[0].IsTrue : true;
      var result = new StringBuilder();

      foreach (var (key, value) in map) {
        if (value.Kind == ValueKind.None || value.Kind == ValueKind.Undefined) {
          continue;
        }

        if (result.Length > 0 || autospace) {
          result.Append(' ');
        }

        result.Append(HttpUtility.HtmlEncode(key));
        result.Append("=\"");
        result.Append(HttpUtility.HtmlEncode(value.AsString()));
        result.Append('"');
      }

      return Value.FromSafeString(result.ToString());
    },
  }).ToFrozenDictionary();

  private static Value GetAttr(Value obj, string attr) {
    if (obj.Kind == ValueKind.Map) {
      var map = obj.AsMap();
      if (map.TryGetValue(attr, out var v)) {
        return v;
      }
    } else if (obj.TryGetAttr(attr, out var attrValue)) {
      return attrValue;
    }

    return Value.FromUndefined();
  }

  private static int CompareValues(Value a, Value b) {
    if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number) {
      return a.AsInt().CompareTo(b.AsInt());
    }

    if ((a.Kind == ValueKind.Number || a.Kind == ValueKind.Number) &&
        (b.Kind == ValueKind.Number || b.Kind == ValueKind.Number)) {
      return a.AsFloat().CompareTo(b.AsFloat());
    }

    return string.Compare(a.AsString(), b.AsString(), StringComparison.Ordinal);
  }
}
