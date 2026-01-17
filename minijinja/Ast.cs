namespace MiniJinja;

/// <summary>
/// Base class for AST nodes.
/// </summary>
public abstract class Node { }

// Expressions
public abstract class Expr : Node { }

public class LiteralExpr(Value value) : Expr {
  public Value Value { get; } = value;
}

public class VarExpr(string name) : Expr {
  public string Name { get; } = name;
}

public class BinaryExpr(string op, Expr left, Expr right) : Expr {
  public string Op { get; } = op;
  public Expr Left { get; } = left;
  public Expr Right { get; } = right;
}

public class UnaryExpr(string op, Expr expr) : Expr {
  public string Op { get; } = op;
  public Expr Expr { get; } = expr;
}

public class GetAttrExpr(Expr obj, string attr) : Expr {
  public Expr Object { get; } = obj;
  public string Attr { get; } = attr;
}

public class GetItemExpr(Expr obj, Expr index) : Expr {
  public Expr Object { get; } = obj;
  public Expr Index { get; } = index;
}

public class SliceExpr(Expr obj, Expr? start, Expr? stop, Expr? step) : Expr {
  public Expr Object { get; } = obj;
  public Expr? Start { get; } = start;
  public Expr? Stop { get; } = stop;
  public Expr? Step { get; } = step;
}

public class CallExpr(Expr callee, List<Expr> args, Dictionary<string, Expr> kwargs) : Expr {
  public Expr Callee { get; } = callee;
  public List<Expr> Args { get; } = args;
  public Dictionary<string, Expr> Kwargs { get; } = kwargs;
}

public class FilterExpr(Expr expr, string name, List<Expr> args, Dictionary<string, Expr> kwargs) : Expr {
  public Expr Expr { get; } = expr;
  public string Name { get; } = name;
  public List<Expr> Args { get; } = args;
  public Dictionary<string, Expr> Kwargs { get; } = kwargs;
}

public class TestExpr(Expr expr, string name, List<Expr> args, bool negated) : Expr {
  public Expr Expr { get; } = expr;
  public string Name { get; } = name;
  public List<Expr> Args { get; } = args;
  public bool Negated { get; } = negated;
}

public class ConditionalExpr(Expr condition, Expr trueExpr, Expr falseExpr) : Expr {
  public Expr Condition { get; } = condition;
  public Expr TrueExpr { get; } = trueExpr;
  public Expr FalseExpr { get; } = falseExpr;
}

public class ListExpr(List<Expr> items) : Expr {
  public List<Expr> Items { get; } = items;
}

public class DictExpr(List<(Expr Key, Expr Value)> items) : Expr {
  public List<(Expr Key, Expr Value)> Items { get; } = items;
}

// Statements
public abstract class Stmt : Node { }

public class TemplateDataStmt(string data) : Stmt {
  public string Data { get; } = data;
}

public class EmitExprStmt(Expr expr) : Stmt {
  public Expr Expr { get; } = expr;
}

public class ForLoopStmt(string target, string? target2, Expr iter, Expr? filter, List<Stmt> body, List<Stmt> elseBody, bool recursive) : Stmt {
  public string Target { get; } = target;
  public string? Target2 { get; } = target2;
  public Expr Iter { get; } = iter;
  public Expr? Filter { get; } = filter;
  public List<Stmt> Body { get; } = body;
  public List<Stmt> ElseBody { get; } = elseBody;
  public bool Recursive { get; } = recursive;
}

public class IfStmt(Expr condition, List<Stmt> trueBody, List<Stmt> falseBody) : Stmt {
  public Expr Condition { get; } = condition;
  public List<Stmt> TrueBody { get; } = trueBody;
  public List<Stmt> FalseBody { get; } = falseBody;
}

public class SetStmt(string target, string? attr, Expr value) : Stmt {
  public string Target { get; } = target;
  public string? Attr { get; } = attr;
  public Expr Value { get; } = value;
}

public class WithStmt(List<(string Name, Expr Value)> bindings, List<Stmt> body) : Stmt {
  public List<(string Name, Expr Value)> Bindings { get; } = bindings;
  public List<Stmt> Body { get; } = body;
}

public class MacroStmt(string name, List<(string Name, Expr? Default)> args, List<Stmt> body) : Stmt {
  public string Name { get; } = name;
  public List<(string Name, Expr? Default)> Args { get; } = args;
  public List<Stmt> Body { get; } = body;
}

public class CallBlockStmt(CallExpr call, List<(string Name, Expr? Default)> args, List<Stmt> body) : Stmt {
  public CallExpr Call { get; } = call;
  public List<(string Name, Expr? Default)> Args { get; } = args;
  public List<Stmt> Body { get; } = body;
}

public class BlockStmt(string name, List<Stmt> body) : Stmt {
  public string Name { get; } = name;
  public List<Stmt> Body { get; } = body;
}

public class ExtendsStmt(Expr name) : Stmt {
  public Expr Name { get; } = name;
}

public class IncludeStmt(Expr name, bool ignoreMissing) : Stmt {
  public Expr Name { get; } = name;
  public bool IgnoreMissing { get; } = ignoreMissing;
}

public class ImportStmt(Expr template, string asName) : Stmt {
  public Expr Template { get; } = template;
  public string AsName { get; } = asName;
}

public class FromImportStmt(Expr template, List<(string Name, string? Alias)> names) : Stmt {
  public Expr Template { get; } = template;
  public List<(string Name, string? Alias)> Names { get; } = names;
}

public class FilterBlockStmt(string name, List<Expr> args, List<Stmt> body) : Stmt {
  public string Name { get; } = name;
  public List<Expr> Args { get; } = args;
  public List<Stmt> Body { get; } = body;
}

public class TemplateNode(List<Stmt> body) : Node {
  public List<Stmt> Body { get; } = body;
}
