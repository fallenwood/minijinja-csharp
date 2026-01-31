using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniJinja.SourceGenerator;

/// <summary>
/// Represents a member (property or field) that can be serialized.
/// </summary>
internal readonly struct MemberInfo {
  public string Name { get; }
  public ITypeSymbol Type { get; }
  public ISymbol Symbol { get; }

  public MemberInfo(string name, ITypeSymbol type, ISymbol symbol) {
    Name = name;
    Type = type;
    Symbol = symbol;
  }
}

/// <summary>
/// Source generator for MiniJinja context types.
/// Automatically implements ITemplateSerializable for types marked with [MiniJinjaContext].
///
/// Diagnostics:
/// - MINIJINJA001 (Warning): Property name collision after applying naming strategy
/// - MINIJINJA002 (Error): Type with [MiniJinjaContext] must be partial
/// </summary>
[Generator]
public class MiniJinjaContextGenerator : IIncrementalGenerator {
  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // Find all types marked with [MiniJinjaContext]
    var classDeclarations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
        transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
      .Where(static m => m.HasValue);

    context.RegisterSourceOutput(classDeclarations, static (spc, typeInfo) => {
      if (typeInfo.HasValue) {
        Execute(typeInfo.Value, spc);
      }
    });
  }

  private static bool IsSyntaxTargetForGeneration(SyntaxNode node) {
    return node is TypeDeclarationSyntax typeDeclaration &&
           typeDeclaration.AttributeLists.Count > 0;
  }

  private static (INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax)? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
    var typeDeclaration = (TypeDeclarationSyntax)context.Node;

    foreach (var attributeList in typeDeclaration.AttributeLists) {
      foreach (var attribute in attributeList.Attributes) {
        var symbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;
        if (symbol is not IMethodSymbol attributeSymbol) {
          continue;
        }

        var attributeType = attributeSymbol.ContainingType;
        var fullName = attributeType.ToDisplayString();

        if (fullName == "MiniJinja.MiniJinjaContextAttribute") {
          var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
          if (typeSymbol is not null) {
            return (typeSymbol, typeDeclaration);
          }
        }
      }
    }

    return null;
  }

  private static void Execute((INamedTypeSymbol Symbol, TypeDeclarationSyntax Syntax) typeInfo, SourceProductionContext context) {
    var typeSymbol = typeInfo.Symbol;
    var typeSyntax = typeInfo.Syntax;

    // Check if the type is declared as partial
    var isPartial = typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    if (!isPartial) {
      var descriptor = new DiagnosticDescriptor(
        id: "MINIJINJA002",
        title: "Type with [MiniJinjaContext] must be partial",
        messageFormat: "The type '{0}' is marked with [MiniJinjaContext] but is not declared as partial. Add the 'partial' modifier to the type declaration.",
        category: "MiniJinja.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

      var location = typeSyntax.Identifier.GetLocation();
      var diagnostic = Diagnostic.Create(descriptor, location, typeSymbol.Name);
      context.ReportDiagnostic(diagnostic);
      return; // Don't generate code if the type isn't partial
    }

    // Get the MiniJinjaContext attribute to read naming strategy
    var contextAttribute = typeSymbol.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MiniJinja.MiniJinjaContextAttribute");

    var namingStrategy = 0; // Default to CamelCase
    if (contextAttribute is not null) {
      foreach (var namedArg in contextAttribute.NamedArguments.Where(na => na.Key == "KeyNamingStrategy")) {
        if (namedArg.Value.Value is int strategyValue) {
          namingStrategy = strategyValue;
        }
      }
    }

    // Get all public properties
    var properties = typeSymbol.GetMembers()
      .OfType<IPropertySymbol>()
      .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                  p.GetMethod is not null &&
                  !p.IsStatic)
      .Select(p => new MemberInfo(p.Name, p.Type, p))
      .ToList();

    // Get all public fields
    var fields = typeSymbol.GetMembers()
      .OfType<IFieldSymbol>()
      .Where(f => f.DeclaredAccessibility == Accessibility.Public &&
                  !f.IsStatic &&
                  !f.IsConst)
      .Select(f => new MemberInfo(f.Name, f.Type, f))
      .ToList();

    var members = properties.Concat(fields).ToList();

    if (members.Count == 0) {
      return;
    }

    // Check for member name collisions after applying naming strategy
    var memberKeyMap = new Dictionary<string, List<string>>();

    foreach (var member in members) {
      // Check for MiniJinjaProperty attribute
      var propertyAttribute = member.Symbol.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MiniJinja.MiniJinjaPropertyAttribute");

      // Check if member should be ignored
      var ignore = false;
      string? customName = null;

      if (propertyAttribute is not null) {
        foreach (var namedArg in propertyAttribute.NamedArguments) {
          if (namedArg.Key == "Ignore" && namedArg.Value.Value is bool ignoreValue) {
            ignore = ignoreValue;
          } else if (namedArg.Key == "Name" && namedArg.Value.Value is string nameValue) {
            customName = nameValue;
          }
        }
      }

      if (ignore) {
        continue;
      }

      var memberName = member.Name;
      var keyName = customName ?? ConvertPropertyName(memberName, namingStrategy);

      if (!memberKeyMap.ContainsKey(keyName)) {
        memberKeyMap[keyName] = new List<string>();
      }
      memberKeyMap[keyName].Add(memberName);
    }

    // Report diagnostics for any collisions
    foreach (var kvp in memberKeyMap.Where(kvp => kvp.Value.Count > 1)) {
      var keyName = kvp.Key;
      var memberNames = kvp.Value;

      var descriptor = new DiagnosticDescriptor(
        id: "MINIJINJA001",
        title: "Member name collision after applying naming strategy",
        messageFormat: "Members {0} all map to the same key '{1}' after applying the naming strategy. This will cause the last member to overwrite previous ones. Consider using [MiniJinjaProperty(Name = \"...\")] to provide unique names.",
        category: "MiniJinja.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

      var location = typeInfo.Syntax.GetLocation();
      var diagnostic = Diagnostic.Create(descriptor, location, string.Join(", ", memberNames.Select(p => $"'{p}'")), keyName);
      context.ReportDiagnostic(diagnostic);
    }

    var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
      ? null
      : typeSymbol.ContainingNamespace.ToDisplayString();

    var typeName = typeSymbol.Name;
    var typeKind = typeInfo.Syntax.Keyword.Text; // "class" or "struct"

    var source = GenerateSource(namespaceName, typeName, typeKind, members, namingStrategy);
    context.AddSource($"{typeName}.g.cs", source);
  }

  private static string GenerateSource(
    string? namespaceName,
    string typeName,
    string typeKind,
    List<MemberInfo> members,
    int namingStrategy) {

    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();

    if (namespaceName is not null) {
      sb.AppendLine($"namespace {namespaceName};");
      sb.AppendLine();
    }

    sb.AppendLine($"partial {typeKind} {typeName} : MiniJinja.ITemplateSerializable {{");
    sb.AppendLine("  public System.Collections.Generic.Dictionary<string, MiniJinja.Value> ToTemplateValues() {");
    sb.AppendLine("    return new System.Collections.Generic.Dictionary<string, MiniJinja.Value> {");

    foreach (var member in members) {
      // Check for MiniJinjaProperty attribute
      var propertyAttribute = member.Symbol.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MiniJinja.MiniJinjaPropertyAttribute");

      // Check if member should be ignored
      var ignore = false;
      string? customName = null;

      if (propertyAttribute is not null) {
        foreach (var namedArg in propertyAttribute.NamedArguments) {
          if (namedArg.Key == "Ignore" && namedArg.Value.Value is bool ignoreValue) {
            ignore = ignoreValue;
          } else if (namedArg.Key == "Name" && namedArg.Value.Value is string nameValue) {
            customName = nameValue;
          }
        }
      }

      if (ignore) {
        continue;
      }

      var memberName = member.Name;
      var keyName = customName ?? ConvertPropertyName(memberName, namingStrategy);

      sb.Append($"      [\"{keyName}\"] = ");

      // Determine the appropriate Value.From* method based on the member type
      var typeInfo = member.Type;
      var conversionMethod = GetConversionMethod(typeInfo);

      sb.AppendLine($"{conversionMethod}({memberName}),");
    }

    sb.AppendLine("    };");
    sb.AppendLine("  }");
    sb.AppendLine("}");

    return sb.ToString();
  }

  private static string ConvertPropertyName(string propertyName, int namingStrategy) {
    // 0 = CamelCase, 1 = SnakeCase, 2 = KebabCase, 3 = None
    return namingStrategy switch {
      1 => ToSnakeCase(propertyName),
      2 => ToKebabCase(propertyName),
      3 => propertyName,
      _ => ToCamelCase(propertyName)
    };
  }

  private static string ToCamelCase(string name) {
    if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) {
      return name;
    }
    return char.ToLowerInvariant(name[0]) + name.Substring(1);
  }

  private static string ToSnakeCase(string name) {
    if (string.IsNullOrEmpty(name)) {
      return name;
    }

    var sb = new StringBuilder();
    sb.Append(char.ToLowerInvariant(name[0]));

    for (int i = 1; i < name.Length; i++) {
      var c = name[i];
      if (char.IsUpper(c)) {
        sb.Append('_');
        sb.Append(char.ToLowerInvariant(c));
      } else {
        sb.Append(c);
      }
    }

    return sb.ToString();
  }

  private static string ToKebabCase(string name) {
    if (string.IsNullOrEmpty(name)) {
      return name;
    }

    var sb = new StringBuilder();
    sb.Append(char.ToLowerInvariant(name[0]));

    for (int i = 1; i < name.Length; i++) {
      var c = name[i];
      if (char.IsUpper(c)) {
        sb.Append('-');
        sb.Append(char.ToLowerInvariant(c));
      } else {
        sb.Append(c);
      }
    }

    return sb.ToString();
  }

  private static string GetConversionMethod(ITypeSymbol typeSymbol) {
    // Check for nullable reference or value types - use FromAny which handles null correctly
    if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) {
      return "MiniJinja.Value.FromAny";
    }

    // Check for Nullable<T> value types (int?, bool?, etc.)
    if (typeSymbol is INamedTypeSymbol namedType &&
        namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
      return "MiniJinja.Value.FromAny";
    }

    var typeName = typeSymbol.ToDisplayString();

    return typeName switch {
      "string" => "MiniJinja.Value.FromString",
      "bool" => "MiniJinja.Value.FromBool",
      "int" => "MiniJinja.Value.FromInt",
      "long" => "MiniJinja.Value.FromInt",
      "float" => "MiniJinja.Value.FromDouble",
      "double" => "MiniJinja.Value.FromDouble",
      _ when typeSymbol.IsValueType => "MiniJinja.Value.FromAny",
      _ => "MiniJinja.Value.FromAny"
    };
  }
}
