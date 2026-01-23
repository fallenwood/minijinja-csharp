namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class PropertyAttributeTests {
  [Fact]
  public void CustomPropertyName_ShouldUseSpecifiedName() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ user_name }} is {{ user_age }} years old");
    var person = new PersonWithCustomNames { Name = "Alice", Age = 30 };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("Alice is 30 years old");
  }

  [Fact]
  public void IgnoredProperty_ShouldNotAppearInTemplate() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("Name: {{ name }}, Password: {{ password }}");
    var user = new UserWithIgnoredProperty { Name = "Bob", Password = "secret123" };

    // Act
    var result = tmpl.Render(user);

    // Assert
    result.Should().Be("Name: Bob, Password: ");
  }

  [Fact]
  public void SnakeCaseNaming_ShouldConvertToSnakeCase() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ first_name }} {{ last_name }}");
    var person = new PersonWithSnakeCase { FirstName = "John", LastName = "Doe" };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("John Doe");
  }

  [Fact]
  public void KebabCaseNaming_ShouldConvertToKebabCase() {
    // Arrange
    var env = new Environment();
    // Note: Kebab-case requires bracket notation since hyphens can't be used in dot notation
    var tmpl = env.TemplateFromString("{{ person['first-name'] }} {{ person['last-name'] }}");
    var person = new PersonWithKebabCase { FirstName = "Jane", LastName = "Smith" };

    // Act
    var result = tmpl.Render(new Dictionary<string, object?> { ["person"] = person });

    // Assert
    result.Should().Be("Jane Smith");
  }

  [Fact]
  public void NoneNaming_ShouldKeepOriginalPropertyNames() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ FirstName }} {{ LastName }}");
    var person = new PersonWithNoneNaming { FirstName = "Alice", LastName = "Johnson" };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("Alice Johnson");
  }

  [Fact]
  public void MixedAttributes_ShouldRespectCustomNameOverNamingStrategy() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ full_name }} is {{ user_age }} in {{ city }}");
    var person = new PersonWithMixedAttributes {
      FirstName = "Charlie",
      Age = 25,
      City = "Seattle"
    };

    // Act
    var result = tmpl.Render(person);

    // Assert
    result.Should().Be("Charlie is 25 in Seattle");
  }

  [Fact]
  public void ComplexObjectWithSnakeCase_ShouldWorkWithNestedObjects() {
    // Arrange
    var env = new Environment();
    var tmpl = env.TemplateFromString(@"
Company: {{ company_name }}
CEO: {{ ceo.first_name }} {{ ceo.last_name }}
".Trim());

    var company = new CompanyWithSnakeCase {
      CompanyName = "TechCorp",
      Ceo = new CeoWithSnakeCase { FirstName = "John", LastName = "Smith" }
    };

    // Act
    var result = tmpl.Render(company);

    // Assert
    result.Should().Be(@"
Company: TechCorp
CEO: John Smith
".Trim());
  }
}

// Test classes
[MiniJinjaContext]
partial class PersonWithCustomNames {
  [MiniJinjaProperty(Name = "user_name")]
  public string Name { get; set; } = "";

  [MiniJinjaProperty(Name = "user_age")]
  public int Age { get; set; }
}

[MiniJinjaContext]
partial class UserWithIgnoredProperty {
  public string Name { get; set; } = "";

  [MiniJinjaProperty(Ignore = true)]
  public string Password { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.SnakeCase)]
partial class PersonWithSnakeCase {
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.KebabCase)]
partial class PersonWithKebabCase {
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.None)]
partial class PersonWithNoneNaming {
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.SnakeCase)]
partial class PersonWithMixedAttributes {
  [MiniJinjaProperty(Name = "full_name")]
  public string FirstName { get; set; } = "";

  [MiniJinjaProperty(Name = "user_age")]
  public int Age { get; set; }

  // This should still use snake_case from the class-level strategy
  public string City { get; set; } = "";
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.SnakeCase)]
partial class CompanyWithSnakeCase {
  public string CompanyName { get; set; } = "";
  public CeoWithSnakeCase Ceo { get; set; } = new();
}

[MiniJinjaContext(KeyNamingStrategy = KeyNamingStrategy.SnakeCase)]
partial class CeoWithSnakeCase {
  public string FirstName { get; set; } = "";
  public string LastName { get; set; } = "";
}
