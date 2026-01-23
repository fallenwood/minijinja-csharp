namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class MiniJinjaContextTests {
  [Fact]
  public void SimpleObject_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ name }} is {{ age }} years old");
    var person = new SimplePerson { Name = "Alice", Age = 30 };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("Alice is 30 years old");
  }

  [Fact]
  public void ObjectWithAllPrimitiveTypes_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ str }} {{ intVal }} {{ longVal }} {{ doubleVal }} {{ boolVal }}");
    var obj = new AllTypesObject {
      Str = "hello",
      IntVal = 42,
      LongVal = 9999999999L,
      FloatVal = 3.14f,
      DoubleVal = 2.71828,
      BoolVal = true
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("hello 42 9999999999 2.71828 true");
  }

  [Fact]
  public void ObjectWithNullableProperties_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("Name: {{ name }}, Age: {{ age }}, City: {{ city }}");
    var person = new PersonWithNullables {
      Name = "Bob",
      Age = null,
      City = "Seattle"
    };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("Name: Bob, Age: , City: Seattle");
  }

  [Fact]
  public void ObjectWithCollections_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("Tags: {{ tags|join(', ') }}, Count: {{ scores|length }}");
    var obj = new ObjectWithCollections {
      Tags = new[] { "csharp", "dotnet", "template" },
      Scores = new List<int> { 95, 87, 92 }
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("Tags: csharp, dotnet, template, Count: 3");
  }

  [Fact]
  public void NestedObject_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ user.name }} works at {{ user.company.name }} in {{ user.company.location }}");
    var obj = new UserWithCompany {
      User = new UserInfo {
        Name = "Charlie",
        Company = new CompanyInfo {
          Name = "Tech Corp",
          Location = "San Francisco"
        }
      }
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("Charlie works at Tech Corp in San Francisco");
  }

  [Fact]
  public void ObjectWithDictionary_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ name }}: {{ metadata.role }}, {{ metadata.level }}");
    var obj = new ObjectWithDictionary {
      Name = "Diana",
      Metadata = new Dictionary<string, object?> {
        ["role"] = "Engineer",
        ["level"] = "Senior"
      }
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("Diana: Engineer, Senior");
  }

  [Fact]
  public void ObjectInLoop_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items %}{{ item.name }}: {{ item.value }}{% if not loop.last %}, {% endif %}{% endfor %}");
    var obj = new ObjectWithItems {
      Items = new[] {
        new Item { Name = "apple", Value = 1 },
        new Item { Name = "banana", Value = 2 },
        new Item { Name = "cherry", Value = 3 }
      }
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("apple: 1, banana: 2, cherry: 3");
  }

  [Fact]
  public void ObjectWithReadOnlyProperties_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ firstName }} {{ lastName }} ({{ fullName }})");
    var person = new PersonWithReadOnly("John", "Doe");

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("John Doe (John Doe)");
  }

  [Fact]
  public void ObjectWithInitOnlyProperties_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ id }}: {{ name }}");
    var obj = new ObjectWithInitOnly {
      Id = 123,
      Name = "Test"
    };

    // Act
    var result = tmpl.Render(obj);

    // Assert
    result.Should().Be("123: Test");
  }

  [Fact]
  public void ComplexNestedStructure_ShouldRenderCorrectly() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"
Organization: {{ name }}
Teams:
{% for team in teams %}
  - {{ team.name }}:
    {% for member in team.members %}{{ member.name }}{% if not loop.last %}, {% endif %}{% endfor %}
{% endfor %}
".Trim());

    var org = new Organization {
      Name = "Acme Corp",
      Teams = new[] {
        new Team {
          Name = "Engineering",
          Members = new[] {
            new Member { Name = "Alice" },
            new Member { Name = "Bob" }
          }
        },
        new Team {
          Name = "Sales",
          Members = new[] {
            new Member { Name = "Charlie" }
          }
        }
      }
    };

    // Act
    var result = tmpl.Render(org);

    // Assert
    result.Should().Contain("Organization: Acme Corp");
    result.Should().Contain("Engineering");
    result.Should().Contain("Alice, Bob");
    result.Should().Contain("Sales");
    result.Should().Contain("Charlie");
  }
}

// Test models with MiniJinjaContext attribute

[MiniJinjaContext]
partial class SimplePerson {
  public string Name { get; set; } = "";
  public int Age { get; set; }
}

[MiniJinjaContext]
partial class AllTypesObject {
  public string Str { get; set; } = "";
  public int IntVal { get; set; }
  public long LongVal { get; set; }
  public float FloatVal { get; set; }
  public double DoubleVal { get; set; }
  public bool BoolVal { get; set; }
}

[MiniJinjaContext]
partial class PersonWithNullables {
  public string Name { get; set; } = "";
  public int? Age { get; set; }
  public string City { get; set; } = "";
}

[MiniJinjaContext]
partial class ObjectWithCollections {
  public string[] Tags { get; set; } = Array.Empty<string>();
  public List<int> Scores { get; set; } = new();
}

[MiniJinjaContext]
partial class CompanyInfo {
  public string Name { get; set; } = "";
  public string Location { get; set; } = "";
}

[MiniJinjaContext]
partial class UserInfo {
  public string Name { get; set; } = "";
  public CompanyInfo? Company { get; set; }
}

[MiniJinjaContext]
partial class UserWithCompany {
  public UserInfo? User { get; set; }
}

[MiniJinjaContext]
partial class ObjectWithDictionary {
  public string Name { get; set; } = "";
  public Dictionary<string, object?>? Metadata { get; set; }
}

[MiniJinjaContext]
partial class Item {
  public string Name { get; set; } = "";
  public int Value { get; set; }
}

[MiniJinjaContext]
partial class ObjectWithItems {
  public Item[] Items { get; set; } = Array.Empty<Item>();
}

[MiniJinjaContext]
partial class PersonWithReadOnly {
  public string FirstName { get; }
  public string LastName { get; }
  public string FullName => $"{FirstName} {LastName}";

  public PersonWithReadOnly(string firstName, string lastName) {
    FirstName = firstName;
    LastName = lastName;
  }
}

[MiniJinjaContext]
partial class ObjectWithInitOnly {
  public int Id { get; init; }
  public string Name { get; init; } = "";
}

[MiniJinjaContext]
partial class Member {
  public string Name { get; set; } = "";
}

[MiniJinjaContext]
partial class Team {
  public string Name { get; set; } = "";
  public Member[] Members { get; set; } = Array.Empty<Member>();
}

[MiniJinjaContext]
partial class Organization {
  public string Name { get; set; } = "";
  public Team[] Teams { get; set; } = Array.Empty<Team>();
}
