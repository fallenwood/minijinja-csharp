namespace MiniJinja;

using System;

/// <summary>
/// The MiniJinja template environment.
/// </summary>
public class Environment : IDisposable {
  private readonly Dictionary<string, Template> templates = [];
  private readonly Dictionary<string, Value> globals = [];
  private readonly Dictionary<string, Func<Value, List<Value>, Dictionary<string, Value>, State, Value>> filters = [];
  private readonly Dictionary<string, Func<Value, List<Value>, bool>> tests = [];
  private readonly Dictionary<string, Func<List<Value>, Dictionary<string, Value>, State, Value>> functions = [];
  private bool disposed;

  /// <summary>
  /// Gets the version of the MiniJinja library.
  /// </summary>
  public static string Version => "2.14.0";

  /// <summary>
  /// Creates a new template environment.
  /// </summary>
  public Environment() {
  }

  /// <summary>
  /// Creates a template from a string.
  /// </summary>
  /// <param name="source">The template source code.</param>
  /// <returns>The compiled template.</returns>
  public Template TemplateFromString(string source) {
    return TemplateFromNamedString("<string>", source);
  }

  /// <summary>
  /// Creates a template from a string with a specified name.
  /// </summary>
  /// <param name="name">The template name.</param>
  /// <param name="source">The template source code.</param>
  /// <returns>The compiled template.</returns>
  public Template TemplateFromNamedString(string name, string source) {
    ThrowIfDisposed();
    return new Template(this, name, source);
  }

  /// <summary>
  /// Adds a template to the environment.
  /// </summary>
  /// <param name="name">The template name.</param>
  /// <param name="source">The template source code.</param>
  public void AddTemplate(string name, string source) {
    ThrowIfDisposed();
    templates[name] = new Template(this, name, source);
  }

  /// <summary>
  /// Gets a template by name.
  /// </summary>
  /// <param name="name">The template name.</param>
  /// <returns>The template.</returns>
  public Template GetTemplate(string name) {
    ThrowIfDisposed();
    if (templates.TryGetValue(name, out var template)) {
      return template;
    }
    throw new TemplateError($"Template not found: {name}");
  }

  /// <summary>
  /// Adds a global variable to the environment.
  /// </summary>
  /// <param name="name">The variable name.</param>
  /// <param name="value">The value.</param>
  public void AddGlobal(string name, object? value) {
    ThrowIfDisposed();
    globals[name] = Value.FromAny(value);
  }

  /// <summary>
  /// Tries to get a global variable.
  /// </summary>
  public bool TryGetGlobal(string name, out Value value) {
    return globals.TryGetValue(name, out value);
  }

  /// <summary>
  /// Adds a custom filter to the environment.
  /// </summary>
  /// <param name="name">The filter name.</param>
  /// <param name="filter">The filter function.</param>
  public void AddFilter(string name, Func<Value, List<Value>, Dictionary<string, Value>, State, Value> filter) {
    ThrowIfDisposed();
    filters[name] = filter;
  }

  /// <summary>
  /// Adds a simple custom filter (no kwargs).
  /// </summary>
  public void AddFilter(string name, Func<Value, Value> filter) {
    AddFilter(name, (v, _, _, _) => filter(v));
  }

  /// <summary>
  /// Adds a simple custom filter with args.
  /// </summary>
  public void AddFilter(string name, Func<Value, List<Value>, Value> filter) {
    AddFilter(name, (v, args, _, _) => filter(v, args));
  }

  /// <summary>
  /// Tries to get a custom filter.
  /// </summary>
  public bool TryGetFilter(string name, out Func<Value, List<Value>, Dictionary<string, Value>, State, Value> filter) {
    return filters.TryGetValue(name, out filter!);
  }

  /// <summary>
  /// Adds a custom test to the environment.
  /// </summary>
  /// <param name="name">The test name.</param>
  /// <param name="test">The test function.</param>
  public void AddTest(string name, Func<Value, List<Value>, bool> test) {
    ThrowIfDisposed();
    tests[name] = test;
  }

  /// <summary>
  /// Adds a simple custom test.
  /// </summary>
  public void AddTest(string name, Func<Value, bool> test) {
    AddTest(name, (v, _) => test(v));
  }

  /// <summary>
  /// Checks if a test exists.
  /// </summary>
  public bool HasTest(string name) {
    return tests.ContainsKey(name) || BuiltinTests.Tests.ContainsKey(name);
  }

  /// <summary>
  /// Runs a custom test.
  /// </summary>
  public bool RunTest(string name, Value value, List<Value> args) {
    if (tests.TryGetValue(name, out var test)) {
      return test(value, args);
    }
    throw new TemplateError($"Unknown test: {name}");
  }

  /// <summary>
  /// Adds a custom function to the environment.
  /// </summary>
  /// <param name="name">The function name.</param>
  /// <param name="function">The function.</param>
  public void AddFunction(string name, Func<List<Value>, Dictionary<string, Value>, State, Value> function) {
    ThrowIfDisposed();
    functions[name] = function;
    globals[name] = Value.FromCallable((args, kwargs, state) => function(args, kwargs, state));
  }

  /// <summary>
  /// Adds a simple custom function.
  /// </summary>
  public void AddFunction(string name, Func<Value> function) {
    AddFunction(name, (_, _, _) => function());
  }

  /// <summary>
  /// Adds a simple custom function with args.
  /// </summary>
  public void AddFunction(string name, Func<List<Value>, Value> function) {
    AddFunction(name, (args, _, _) => function(args));
  }

  private void ThrowIfDisposed() {
    ObjectDisposedException.ThrowIf(disposed, this);
  }

  /// <summary>
  /// Disposes the environment.
  /// </summary>
  public void Dispose() {
    if (!disposed) {
      disposed = true;
      templates.Clear();
      globals.Clear();
      filters.Clear();
      tests.Clear();
      functions.Clear();
    }
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// A compiled template.
/// </summary>
public class Template {
  private readonly Environment _env;
  private readonly string _name;
  private readonly TemplateNode _ast;

  internal Template(Environment env, string name, string source) {
    _env = env;
    _name = name;

    var lexer = new Lexer(source);
    var tokens = lexer.Tokenize();
    var parser = new Parser(tokens);
    _ast = parser.Parse();
  }

  internal Template(Environment env, string name) {
    _env = env;
    _name = name;
    _ast = env.GetTemplate(name).Ast;
  }

  internal TemplateNode Ast => _ast;

  /// <summary>
  /// Renders the template with the given context.
  /// </summary>
  /// <param name="context">The context object or dictionary.</param>
  /// <returns>The rendered string.</returns>
  public string Render(IDictionary<string, Value>? context) {
    var state = new State(_env);
    state.CurrentTemplateName = _name;

    if (context != null) {
      foreach (var (key, value) in context) {
        state.Set(key, value);
      }
    }

    var evaluator = new Evaluator(state);
    return evaluator.Evaluate(_ast);
  }

  /// <summary>
  /// Renders the template with no context.
  /// </summary>
  /// <returns>The rendered string.</returns>
  public string Render() {
    return this.Render((IDictionary<string, Value>?)null);
  }

  /// <summary>
  /// Renders the template with the given context.
  /// </summary>
  /// <param name="context">The context object or dictionary.</param>
  /// <returns>The rendered string.</returns>
  public string Render(IDictionary<string, object?> context) {
    var dict = new Dictionary<string, Value>(context.Count);
    foreach (var (key, value) in context) {
      dict[key] = Value.FromAny(value);
    }

    return this.Render(dict);
  }

  /// <summary>
  /// Renders the template with the given context.
  /// </summary>
  /// <param name="context">The context object or dictionary.</param>
  /// <returns>The rendered string.</returns>
  public string Render(ITemplateSerializable context) {
    var dict = context.ToTemplateValues();

    return this.Render(dict);
  }
}
