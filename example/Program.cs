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
  // Using dictionary for context (AOT-compatible)
  var result = tmpl.Render(new Dictionary<string, object?> { ["name"] = "World" });
  Console.WriteLine(result);
}

Section("Template From String"); {
  using var env = new MJEnvironment();
  var tmpl = env.TemplateFromString("Hello {{ name }}!");
  Console.WriteLine(tmpl.Render(new Dictionary<string, object?> { ["name"] = "World" }));
}

Section("Custom Type with ITemplateSerializable"); {
  using var env = new MJEnvironment();
  var tmpl = env.TemplateFromString("{{ name }} is {{ age }} years old");
  var person = new Person { Name = "Alice", Age = 30 };
  Console.WriteLine(tmpl.Render(person));
}

Section("Custom Type with Source Generator"); {
  using var env = new MJEnvironment();
  var tmpl = env.TemplateFromString("{{ name }} is {{ age }} years old and lives in {{ city }}");
  var employee = new Employee { Name = "Bob", Age = 25, City = "Seattle" };
  Console.WriteLine(tmpl.Render(employee));
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
  var output = tmpl.Render(new Dictionary<string, object?> {
    ["name"] = "world",
    ["items"] = new[] { "a", "b", "c" }
  });
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
  Console.WriteLine(tmpl.Render(new Dictionary<string, object?> { ["value"] = "hello" }));
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
  Console.WriteLine(tmpl.Render(new Dictionary<string, object?> { ["word"] = "ha" }));
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

Section("Complex Object with Nested Data"); {
  using var env = new MJEnvironment();
  var tmpl = env.TemplateFromString("""
    Company: {{ name }}
    Departments:
    {% for dept in departments %}
      - {{ dept.name }} ({{ dept.employees|length }} employees)
        {% for emp in dept.employees %}
        * {{ emp.name }}, {{ emp.role }}{% if emp.isManager %} (Manager){% endif %}
        {% endfor %}
    {% endfor %}
    """);

  var company = new Company {
    Name = "Tech Corp",
    Departments = new[] {
      new Department {
        Name = "Engineering",
        Employees = new[] {
          new EmployeeInfo { Name = "Alice", Role = "Senior Engineer", IsManager = true },
          new EmployeeInfo { Name = "Bob", Role = "Engineer", IsManager = false },
          new EmployeeInfo { Name = "Carol", Role = "Junior Engineer", IsManager = false }
        }
      },
      new Department {
        Name = "Sales",
        Employees = new[] {
          new EmployeeInfo { Name = "Dave", Role = "Sales Lead", IsManager = true },
          new EmployeeInfo { Name = "Eve", Role = "Account Manager", IsManager = false }
        }
      }
    }
  };

  Console.WriteLine(tmpl.Render(company).Trim());
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

Section("Property Attributes and Naming Strategies"); {
  using var env = new MJEnvironment();

  // Example 1: Custom property names
  var tmpl1 = env.TemplateFromString("User: {{ user_id }} - {{ display_name }}");
  var user = new UserWithCustomNames { Id = 123, Name = "Alice" };
  Console.WriteLine(tmpl1.Render(user));

  // Example 2: Snake case naming strategy
  var tmpl2 = env.TemplateFromString("{{ first_name }} {{ last_name }} ({{ email_address }})");
  var contact = new ContactWithSnakeCase {
    FirstName = "Bob",
    LastName = "Smith",
    EmailAddress = "bob@example.com"
  };
  Console.WriteLine(tmpl2.Render(contact));

  // Example 3: Ignored properties
  var tmpl3 = env.TemplateFromString("Username: {{ username }}, Password: {{ password }}");
  var credentials = new CredentialsWithIgnore {
    Username = "admin",
    Password = "secret123"
  };
  Console.WriteLine(tmpl3.Render(credentials));
}

// Example of a custom type that implements ITemplateSerializable for AOT compatibility
class Person : ITemplateSerializable {
  public string Name { get; set; } = "";
  public int Age { get; set; }

  public Dictionary<string, Value> ToTemplateValues() {
    return new Dictionary<string, Value> {
      ["name"] = Value.FromString(Name),
      ["age"] = Value.FromInt(Age)
    };
  }
}

// Example of using the source generator - just mark the class with [MiniJinjaContext]
// and make it partial. The ToTemplateValues method will be generated automatically.
[MiniJinjaContext]
partial class Employee {
  public string Name { get; set; } = "";
  public int Age { get; set; }
  public string City { get; set; } = "";
}

// Complex nested object example with collections
[MiniJinjaContext]
partial class Company {
  public string Name { get; set; } = "";
  public Department[] Departments { get; set; } = Array.Empty<Department>();
}

[MiniJinjaContext]
partial class Department {
  public string Name { get; set; } = "";
  public EmployeeInfo[] Employees { get; set; } = Array.Empty<EmployeeInfo>();
}

[MiniJinjaContext]
partial class EmployeeInfo {
  public string Name { get; set; } = "";
  public string Role { get; set; } = "";
  public bool IsManager { get; set; }
}

// Examples for property attributes and naming strategies
[MiniJinjaContext]
partial class UserWithCustomNames {
  [MiniJinjaProperty(Name = "user_id")]
  public int Id { get; set; }

  [MiniJinjaProperty(Name = "display_name")]
  public string Name { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.SnakeCase)]
partial class ContactWithSnakeCase {
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
  public string EmailAddress { get; set; } = "";
}

[MiniJinjaContext]
partial class CredentialsWithIgnore {
  public string Username { get; set; } = "";

  [MiniJinjaProperty(Ignore = true)]
  public string Password { get; set; } = "";
}
