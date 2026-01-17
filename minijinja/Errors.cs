namespace MiniJinja;

/// <summary>
/// Template error exception.
/// </summary>
public class TemplateError : Exception {
  public TemplateError(string message) : base(message) { }
  public TemplateError(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Undefined value exception.
/// </summary>
public class UndefinedError(string name) : TemplateError($"'{name}' is undefined") {
}
