using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

static void Section(string title) {
  Console.WriteLine();
  Console.WriteLine(title);
  Console.WriteLine(new string('-', title.Length));
}

// MiniJinja-CSharp example program.
//
// This is modeled after the quick-start and feature overview shown in
// minijinja-go/minijinja.go (templates, filters, functions, auto-escaping,
// and error handling).

Section("Quick Start"); {
  using var env = new MJEnvironment();
  env.AddTemplate("hello", "Hello {{ name }}!");
  var tmpl = env.GetTemplate("hello");
  var result = tmpl.Render(new { name = "World" });
  Console.WriteLine(result);
}

Section("Template From String"); {
  using var env = new MJEnvironment();
  var tmpl = env.TemplateFromString("Hello {{ name }}!");
  Console.WriteLine(tmpl.Render(new { name = "World" }));
}

Section("Template Syntax (Variables, Blocks, Filters)"); {
  using var env = new MJEnvironment();
  env.AddTemplate(
    "syntax",
    """
		{# Variables and filters #}
		Hello {{ name|upper }}!

		{# Blocks #}
		{% if items %}
		  Count: {{ items|length }}
					Items: {{ items|join(", ") }}
		{% else %}
		  No items.
		{% endif %}
		"""
  );

  var tmpl = env.GetTemplate("syntax");
  var output = tmpl.Render(new { name = "world", items = new[] { "a", "b", "c" } });
  Console.WriteLine(output.Trim());
}

Section("Custom Filter (reverse)"); {
  using var env = new MJEnvironment();

  env.AddFilter("reverse", v => {
    var s = v.AsString();
    var chars = s.ToCharArray();
    Array.Reverse(chars);
    return Value.FromString(new string(chars));
  });

  env.AddTemplate("reverse-demo", "{{ value|reverse }}");
  var tmpl = env.GetTemplate("reverse-demo");
  Console.WriteLine(tmpl.Render(new { value = "hello" }));
}

Section("Custom Function (repeat)"); {
  using var env = new MJEnvironment();

  env.AddFunction("repeat", args => {
    var text = args.Count > 0 ? args[0].AsString() : "";
    var n = args.Count > 1 ? (int)args[1].AsInt() : 1;
    if (n < 0) n = 0;
    return Value.FromString(string.Concat(Enumerable.Repeat(text, n)));
  });

  env.AddTemplate("repeat-demo", "{{ repeat(word, 3) }}");
  var tmpl = env.GetTemplate("repeat-demo");
  Console.WriteLine(tmpl.Render(new { word = "ha" }));
}

Section("Built-in Function (range)"); {
  using var env = new MJEnvironment();
  env.AddTemplate(
    "range-demo",
    """
		{% for i in range(5) %}{{ i }}{% if not loop.last %},{% endif %}{% endfor %}
		"""
  );
  Console.WriteLine(env.GetTemplate("range-demo").Render().Trim());
}

Section("Auto-Escaping and Safe Strings"); {
  using var env = new MJEnvironment();

  // C# binding currently uses HTML escaping by default for emitted values.
  // To bypass escaping for trusted HTML, pass a safe string Value.
  env.AddTemplate("escape-demo", "Unsafe: {{ unsafe }}\nSafe: {{ safe }}");
  var tmpl = env.GetTemplate("escape-demo");

  var ctx = new Dictionary<string, Value> {
    ["unsafe"] = Value.FromString("<foo>"),
    ["safe"] = Value.FromSafeString("<b>ok</b>"),
  };

  Console.WriteLine(tmpl.Render(ctx));
}

Section("Error Handling"); {
  using var env = new MJEnvironment();
  try {
    // This will fail because the template is not registered.
    env.GetTemplate("missing");
  } catch (TemplateError e) {
    Console.WriteLine($"TemplateError: {e.Message}");
  }

  try {
    // This will fail because the template is syntactically invalid.
    env.AddTemplate("bad", "Hello {% if %}");
  } catch (TemplateError e) {
    Console.WriteLine($"TemplateError: {e.Message}");
  }
}
