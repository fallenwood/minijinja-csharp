namespace MiniJinja;

using System.Text;
using System.Web;

/// <summary>
/// Runtime state for template evaluation.
/// </summary>
public class State {
  private readonly List<Dictionary<string, Value>> scopes = [];
  private readonly Dictionary<string, MacroStmt> macros = new();
  private readonly Dictionary<string, List<Stmt>> blocks = new();
  private readonly Dictionary<string, List<Stmt>> parentBlocks = new();
  private readonly Stack<string> blockStack = new();
  private string? extendsTemplate;
  private bool autoEscape = true;

  public Environment Environment { get; }
  public string? CurrentTemplateName { get; set; }

  public State(Environment env) {
    this.Environment = env;
    this.PushScope();
  }

  public void PushScope() {
    scopes.Add([]);
  }

  public void PopScope() {
    if (scopes.Count > 1) {
      scopes.RemoveAt(scopes.Count - 1);
    }
  }

  public void Set(string name, Value value) {
    if (scopes.Count > 0) {
      scopes[^1][name] = value;
    }
  }

  public void SetGlobal(string name, Value value) {
    if (scopes.Count > 0) {
      scopes[0][name] = value;
    }
  }

  public Value Get(string name) {
    // Search scopes from innermost to outermost
    for (var i = scopes.Count - 1; i >= 0; i--) {
      if (scopes[i].TryGetValue(name, out var value)) {
        return value;
      }
    }

    // Check globals from environment
    if (this.Environment.TryGetGlobal(name, out var global)) {
      return global;
    }

    // Check built-in functions
    if (BuiltinFunctions.Functions.TryGetValue(name, out var func)) {
      return Value.FromCallable((args, kwargs, state) => func(args, kwargs, state));
    }

    return Value.FromUndefined();
  }

  public bool TryGet(string name, out Value value) {
    value = this.Get(name);
    return value.Kind != ValueKind.Undefined;
  }

  public Dictionary<string, Value> GetAllVariables() {
    var result = new Dictionary<string, Value>();
    foreach (var scope in scopes) {
      foreach (var (key, value) in scope) {
        result[key] = value;
      }
    }
    return result;
  }

  public void DefineMacro(string name, MacroStmt macro) {
    macros[name] = macro;
  }

  public bool TryGetMacro(string name, out MacroStmt? macro) {
    return macros.TryGetValue(name, out macro);
  }

  public void DefineBlock(string name, List<Stmt> body) {
    blocks[name] = body;
  }

  public bool TryGetBlock(string name, out List<Stmt>? body) {
    return blocks.TryGetValue(name, out body);
  }

  public void DefineParentBlock(string name, List<Stmt> body) {
    parentBlocks[name] = body;
  }

  public bool TryGetParentBlock(string name, out List<Stmt>? body) {
    return parentBlocks.TryGetValue(name, out body);
  }

  public void SetExtends(string templateName) {
    extendsTemplate = templateName;
  }

  public string? GetExtends() => extendsTemplate;

  public void PushBlock(string name) => blockStack.Push(name);
  public void PopBlock() => blockStack.Pop();
  public string? CurrentBlock => blockStack.Count > 0 ? blockStack.Peek() : null;

  public bool AutoEscape {
    get => autoEscape;
    set => autoEscape = value;
  }

  public string AutoEscapeValue(Value value) {
    if (!this.autoEscape) {
      return value.AsString();
    }

    if (value.IsSafe) {
      return value.AsString();
    }

    return HttpUtility.HtmlEncode(value.AsString());
  }
}

/// <summary>
/// Evaluator for executing template AST.
/// </summary>
public class Evaluator(State state) {
  public State State => state;

  public string Evaluate(TemplateNode template) {
    var sb = new StringBuilder();
    this.EvaluateStatements(template.Body, sb);

    // Handle extends recursively
    while (state.GetExtends() != null) {
      var extendsName = state.GetExtends();
      var parentTemplate = state.Environment.GetTemplate(extendsName!);
      var parentAst = parentTemplate.Ast;

      // Clear extends - parent may set it again if it also extends
      state.SetExtends(null!);

      // Evaluate parent template with our blocks
      sb.Clear();
      this.EvaluateStatements(parentAst.Body, sb);
    }

    return sb.ToString();
  }

  public void EvaluateStatements(List<Stmt> statements, StringBuilder sb) {
    foreach (var stmt in statements) {
      this.EvaluateStatement(stmt, sb);
    }
  }

  private void EvaluateStatement(Stmt stmt, StringBuilder sb) {
    switch (stmt) {
      case TemplateDataStmt data:
        sb.Append(data.Data);
        break;

      case EmitExprStmt emit:
        var value = this.EvaluateExpr(emit.Expr);
        if (value.Kind != ValueKind.Undefined && value.Kind != ValueKind.None) {
          sb.Append(state.AutoEscapeValue(value));
        }
        break;

      case ForLoopStmt forLoop:
        this.EvaluateForLoop(forLoop, sb);
        break;

      case IfStmt ifStmt:
        this.EvaluateIf(ifStmt, sb);
        break;

      case SetStmt setStmt:
        this.EvaluateSet(setStmt);
        break;

      case WithStmt withStmt:
        this.EvaluateWith(withStmt, sb);
        break;

      case MacroStmt macro:
        state.DefineMacro(macro.Name, macro);
        break;

      case CallBlockStmt callBlock:
        this.EvaluateCallBlock(callBlock, sb);
        break;

      case BlockStmt block:
        this.EvaluateBlock(block, sb);
        break;

      case ExtendsStmt extends:
        var templateName = EvaluateExpr(extends.Name).AsString();
        state.SetExtends(templateName);
        break;

      case IncludeStmt include:
        this.EvaluateInclude(include, sb);
        break;

      case FilterBlockStmt filterBlock:
        this.EvaluateFilterBlock(filterBlock, sb);
        break;

      case AutoescapeStmt autoescape:
        this.EvaluateStatements(autoescape.Body, sb);
        break;

      case ImportStmt import:
        this.EvaluateImport(import);
        break;

      case FromImportStmt fromImport:
        this.EvaluateFromImport(fromImport);
        break;
    }
  }

  private void EvaluateForLoop(ForLoopStmt forLoop, StringBuilder sb) {
    var iterValue = this.EvaluateExpr(forLoop.Iter);
    var items = this.GetIterableItems(iterValue);

    if (forLoop.Filter != null) {
      items = [.. items.Where(item => {
        state.PushScope();
        this.SetLoopVar(forLoop.Target, forLoop.Target2, item);
        var result = EvaluateExpr(forLoop.Filter).IsTrue;
        state.PopScope();
        return result;
      })];
    }

    if (items.Count == 0) {
      this.EvaluateStatements(forLoop.ElseBody, sb);
      return;
    }

    for (var i = 0; i < items.Count; i++) {
      state.PushScope();

      var item = items[i];
      this.SetLoopVar(forLoop.Target, forLoop.Target2, item);

      // Set loop variable
      var loopObj = new LoopObject(i, items.Count, forLoop.Recursive ? forLoop : null, this, sb);
      state.Set("loop", Value.FromObject(loopObj));

      this.EvaluateStatements(forLoop.Body, sb);

      state.PopScope();
    }
  }

  public void SetLoopVar(string target, string? target2, Value item) {
    if (target2 != null) {
      // Unpacking
      var seq = item.AsSeq();
      state.Set(target, seq.Count > 0 ? seq[0] : Value.FromNone());
      state.Set(target2, seq.Count > 1 ? seq[1] : Value.FromNone());
    } else {
      state.Set(target, item);
    }
  }

  public List<Value> GetIterableItems(Value value) {
    if (value.Kind == ValueKind.Seq) {
      return [.. value.AsSeq()];
    }

    if (value.Kind == ValueKind.Map) {
      return [.. value.AsMap().Select(kv => Value.FromString(kv.Key))];
    }

    if (value.Kind == ValueKind.String) {
      return [.. value.AsString().Select(c => Value.FromString(c.ToString()))];
    }

    if (value.TryIter(out var iter)) {
      return [.. iter];
    }

    return [];
  }

  private void EvaluateIf(IfStmt ifStmt, StringBuilder sb) {
    var condition = this.EvaluateExpr(ifStmt.Condition);
    if (condition.IsTrue) {
      this.EvaluateStatements(ifStmt.TrueBody, sb);
    } else {
      this.EvaluateStatements(ifStmt.FalseBody, sb);
    }
  }

  private void EvaluateSet(SetStmt setStmt) {
    var value = this.EvaluateExpr(setStmt.Value);

    if (setStmt.Attr != null) {
      // Setting an attribute on an object (like namespace)
      var obj = state.Get(setStmt.Target);
      if (obj.RawValue is Namespace ns) {
        ns.SetAttr(setStmt.Attr, value);
      }
    } else {
      state.Set(setStmt.Target, value);
    }
  }

  private void EvaluateWith(WithStmt withStmt, StringBuilder sb) {
    state.PushScope();

    foreach (var (name, expr) in withStmt.Bindings) {
      state.Set(name, this.EvaluateExpr(expr));
    }

    this.EvaluateStatements(withStmt.Body, sb);

    state.PopScope();
  }

  private void EvaluateCallBlock(CallBlockStmt callBlock, StringBuilder sb) {
    // Create a caller function
    var callerBody = callBlock.Body;
    var callerArgs = callBlock.Args;
    var evaluator = this;

    Value CallerFunc(List<Value> args, Dictionary<string, Value> kwargs, State state) {
      state.PushScope();

      for (var i = 0; i < callerArgs.Count; i++) {
        var (name, def) = callerArgs[i];
        if (i < args.Count) {
          state.Set(name, args[i]);
        } else if (def != null) {
          state.Set(name, evaluator.EvaluateExpr(def));
        }
      }

      var bodySb = new StringBuilder();
      evaluator.EvaluateStatements(callerBody, bodySb);
      state.PopScope();

      return Value.FromSafeString(bodySb.ToString());
    }

    state.PushScope();
    state.Set("caller", Value.FromCallable(CallerFunc));

    var result = this.EvaluateFunctionCall(callBlock.Call);
    sb.Append(result.AsString());

    state.PopScope();
  }

  private void EvaluateBlock(BlockStmt block, StringBuilder sb) {
    // Check if there's already an overriding block from child template
    var hasOverride = state.TryGetBlock(block.Name, out var overrideBody);

    // If we're extending and this block is being overridden, 
    // save the parent block body for super()
    if (hasOverride) {
      state.DefineParentBlock(block.Name, block.Body);
    } else {
      // Store the block for inheritance
      state.DefineBlock(block.Name, block.Body);
    }

    // If we're extending, don't render now
    if (state.GetExtends() != null) {
      return;
    }

    // Render the block
    state.PushBlock(block.Name);

    if (hasOverride) {
      this.EvaluateStatements(overrideBody!, sb);
    } else {
      this.EvaluateStatements(block.Body, sb);
    }

    state.PopBlock();
  }

  private void EvaluateInclude(IncludeStmt include, StringBuilder sb) {
    var templateName = this.EvaluateExpr(include.Name).AsString();

    try {
      var template = state.Environment.GetTemplate(templateName);
      var result = template.Render(state.GetAllVariables());
      sb.Append(result);
    } catch (TemplateError) when (include.IgnoreMissing) {
      // Ignore missing templates
    }
  }

  private void EvaluateFilterBlock(FilterBlockStmt filterBlock, StringBuilder sb) {
    var bodySb = new StringBuilder();
    this.EvaluateStatements(filterBlock.Body, bodySb);

    var bodyValue = Value.FromString(bodySb.ToString());

    if (BuiltinFilters.Filters.TryGetValue(filterBlock.Name, out var filter)) {
      var args = filterBlock.Args.Select(this.EvaluateExpr).ToList();
      var result = filter(bodyValue, args, [], state);
      sb.Append(result.AsString());
    } else if (state.Environment.TryGetFilter(filterBlock.Name, out var customFilter)) {
      var args = filterBlock.Args.Select(this.EvaluateExpr).ToList();
      var result = customFilter(bodyValue, args, [], state);
      sb.Append(result.AsString());
    } else {
      throw new TemplateError($"Unknown filter: {filterBlock.Name}");
    }
  }

  private void EvaluateImport(ImportStmt import) {
    var templateName = this.EvaluateExpr(import.Template).AsString();
    var template = state.Environment.GetTemplate(templateName);

    // Create a new state to evaluate the template
    var importState = new State(state.Environment);
    var evaluator = new Evaluator(importState);
    evaluator.Evaluate(template.Ast);

    // Create a module object with the macros
    var module = new ImportedModule(importState);
    state.Set(import.AsName, Value.FromObject(module));
  }

  private void EvaluateFromImport(FromImportStmt fromImport) {
    var templateName = this.EvaluateExpr(fromImport.Template).AsString();
    var template = state.Environment.GetTemplate(templateName);

    // Create a new state to evaluate the template
    var importState = new State(state.Environment);
    var evaluator = new Evaluator(importState);
    evaluator.Evaluate(template.Ast);

    // Import specific names
    foreach (var (name, alias) in fromImport.Names) {
      var targetName = alias ?? name;
      if (importState.TryGetMacro(name, out var macro)) {
        state.DefineMacro(targetName, macro!);
      } else if (importState.TryGet(name, out var value)) {
        state.Set(targetName, value);
      }
    }
  }

  public Value EvaluateExpr(Expr expr) {
    return expr switch {
      LiteralExpr literal => literal.Value,
      VarExpr varExpr => state.Get(varExpr.Name),
      BinaryExpr binary => this.EvaluateBinary(binary),
      UnaryExpr unary => this.EvaluateUnary(unary),
      GetAttrExpr getAttr => this.EvaluateGetAttr(getAttr),
      GetItemExpr getItem => this.EvaluateGetItem(getItem),
      SliceExpr slice => this.EvaluateSlice(slice),
      CallExpr call => this.EvaluateFunctionCall(call),
      FilterExpr filter => this.EvaluateFilter(filter),
      TestExpr test => this.EvaluateTest(test),
      ConditionalExpr cond => this.EvaluateConditional(cond),
      ListExpr list => this.EvaluateList(list),
      DictExpr dict => this.EvaluateDict(dict),
      _ => throw new TemplateError("Unknown expression type")
    };
  }

  private Value EvaluateBinary(BinaryExpr binary) {
    var left = this.EvaluateExpr(binary.Left);

    // Short-circuit evaluation for and/or
    if (binary.Op == "and") {
      if (!left.IsTrue) {
        return Value.FromBool(false);
      }

      return Value.FromBool(this.EvaluateExpr(binary.Right).IsTrue);
    }

    if (binary.Op == "or") {
      if (left.IsTrue) {
        return left;
      }

      return this.EvaluateExpr(binary.Right);
    }

    var right = this.EvaluateExpr(binary.Right);

    return binary.Op switch {
      "+" => Add(left, right),
      "-" => Subtract(left, right),
      "*" => Multiply(left, right),
      "/" => Divide(left, right),
      "//" => FloorDivide(left, right),
      "%" => Modulo(left, right),
      "**" => Power(left, right),
      "~" => Value.FromString(left.AsString() + right.AsString()),
      "==" => Value.FromBool(left.Equals(right)),
      "!=" => Value.FromBool(!left.Equals(right)),
      "<" => Value.FromBool(Compare(left, right) < 0),
      "<=" => Value.FromBool(Compare(left, right) <= 0),
      ">" => Value.FromBool(Compare(left, right) > 0),
      ">=" => Value.FromBool(Compare(left, right) >= 0),
      "in" => Value.FromBool(Contains(right, left)),
      "not in" => Value.FromBool(!Contains(right, left)),
      _ => throw new TemplateError($"Unknown binary operator: {binary.Op}")
    };
  }

  private static Value Add(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) {
      return Value.FromInt(left.AsInt() + right.AsInt());
    }

    if ((left.Kind == ValueKind.Number || left.Kind == ValueKind.Number) &&
        (right.Kind == ValueKind.Number || right.Kind == ValueKind.Number)) {
      return Value.FromFloat(left.AsFloat() + right.AsFloat());
    }

    if (left.Kind == ValueKind.String && right.Kind == ValueKind.String) {
      return Value.FromString(left.AsString() + right.AsString());
    }

    if (left.Kind == ValueKind.Seq && right.Kind == ValueKind.Seq) {
      var result = left.AsSeq().ToList();
      result.AddRange(right.AsSeq());
      return Value.FromSeq(result);
    }

    throw new TemplateError($"Cannot add {left.Kind} and {right.Kind}");
  }

  private static Value Subtract(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) {
      return Value.FromInt(left.AsInt() - right.AsInt());
    }

    return Value.FromFloat(left.AsFloat() - right.AsFloat());
  }

  private static Value Multiply(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) {
      return Value.FromInt(left.AsInt() * right.AsInt());
    }

    if (left.Kind == ValueKind.String && right.Kind == ValueKind.Number) {
      return Value.FromString(string.Concat(Enumerable.Repeat(left.AsString(), (int)right.AsInt())));
    }

    if (left.Kind == ValueKind.Seq && right.Kind == ValueKind.Number) {
      var seq = left.AsSeq();
      var result = new List<Value>();
      for (var i = 0; i < right.AsInt(); i++) {
        result.AddRange(seq);
      }

      return Value.FromSeq(result);
    }

    return Value.FromFloat(left.AsFloat() * right.AsFloat());
  }

  private static Value Divide(Value left, Value right) {
    var r = right.AsFloat();
    if (r == 0) {
      throw new TemplateError("Division by zero");
    }

    return Value.FromFloat(left.AsFloat() / r);
  }

  private static Value FloorDivide(Value left, Value right) {
    var r = right.AsInt();
    if (r == 0) {
      throw new TemplateError("Division by zero");
    }

    return Value.FromInt(left.AsInt() / r);
  }

  private static Value Modulo(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) {
      var r = right.AsInt();
      if (r == 0) {
        throw new TemplateError("Division by zero");
      }

      return Value.FromInt(left.AsInt() % r);
    }

    var rf = right.AsFloat();
    if (rf == 0) {
      throw new TemplateError("Division by zero");
    }

    return Value.FromFloat(left.AsFloat() % rf);
  }

  private static Value Power(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number && right.AsInt() >= 0) {
      return Value.FromInt((long)Math.Pow(left.AsInt(), right.AsInt()));
    }

    return Value.FromFloat(Math.Pow(left.AsFloat(), right.AsFloat()));
  }

  private static int Compare(Value left, Value right) {
    if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number) {
      return left.AsInt().CompareTo(right.AsInt());
    }

    if ((left.Kind == ValueKind.Number || left.Kind == ValueKind.Number) &&
        (right.Kind == ValueKind.Number || right.Kind == ValueKind.Number)) {
      return left.AsFloat().CompareTo(right.AsFloat());
    }

    return string.Compare(left.AsString(), right.AsString(), StringComparison.Ordinal);
  }

  private static bool Contains(Value container, Value item) {
    if (container.Kind == ValueKind.Seq) {
      return container.AsSeq().Any(x => x.Equals(item));
    }

    if (container.Kind == ValueKind.String) {
      return container.AsString().Contains(item.AsString());
    }

    if (container.Kind == ValueKind.Map) {
      return container.AsMap().ContainsKey(item.AsString());
    }

    return false;
  }

  private Value EvaluateUnary(UnaryExpr unary) {
    var value = this.EvaluateExpr(unary.Expr);

    return unary.Op switch {
      "-" => value.Kind == ValueKind.Number
          ? Value.FromInt(-value.AsInt())
          : Value.FromFloat(-value.AsFloat()),
      "+" => value,
      "not" => Value.FromBool(!value.IsTrue),
      _ => throw new TemplateError($"Unknown unary operator: {unary.Op}")
    };
  }

  private Value EvaluateGetAttr(GetAttrExpr getAttr) {
    var obj = this.EvaluateExpr(getAttr.Object);

    if (obj.Kind == ValueKind.Map) {
      var map = obj.AsMap();
      if (map.TryGetValue(getAttr.Attr, out var value)) {
        return value;
      }
    }

    if (obj.TryGetAttr(getAttr.Attr, out var attr)) {
      return attr;
    }

    return Value.FromUndefined();
  }

  private Value EvaluateGetItem(GetItemExpr getItem) {
    var obj = this.EvaluateExpr(getItem.Object);
    var index = this.EvaluateExpr(getItem.Index);

    if (obj.Kind == ValueKind.Seq) {
      var seq = obj.AsSeq();
      var idx = (int)index.AsInt();
      if (idx < 0) {
        idx = seq.Count + idx;
      }

      if (idx >= 0 && idx < seq.Count) {
        return seq[idx];
      }

      return Value.FromUndefined();
    }

    if (obj.Kind == ValueKind.Map) {
      var map = obj.AsMap();
      if (map.TryGetValue(index.AsString(), out var value)) {
        return value;
      }

      return Value.FromUndefined();
    }

    if (obj.Kind == ValueKind.String) {
      var s = obj.AsString();
      var idx = (int)index.AsInt();
      if (idx < 0) {
        idx = s.Length + idx;
      }

      if (idx >= 0 && idx < s.Length) {
        return Value.FromString(s[idx].ToString());
      }

      return Value.FromUndefined();
    }

    return Value.FromUndefined();
  }

  private Value EvaluateSlice(SliceExpr slice) {
    var obj = this.EvaluateExpr(slice.Object);

    int? start = slice.Start != null ? (int)this.EvaluateExpr(slice.Start).AsInt() : null;
    int? stop = slice.Stop != null ? (int)this.EvaluateExpr(slice.Stop).AsInt() : null;
    var step = slice.Step != null ? (int)this.EvaluateExpr(slice.Step).AsInt() : 1;

    if (obj.Kind == ValueKind.Seq) {
      var seq = obj.AsSeq().ToList();
      return Value.FromSeq(SliceList(seq, start, stop, step));
    }

    if (obj.Kind == ValueKind.String) {
      var s = obj.AsString();
      var chars = s.ToCharArray().Select(c => Value.FromString(c.ToString())).ToList();
      var sliced = SliceList(chars, start, stop, step);
      return Value.FromString(string.Concat(sliced.Select(v => v.AsString())));
    }

    return Value.FromUndefined();
  }

  private static List<Value> SliceList(List<Value> list, int? start, int? stop, int step) {
    var len = list.Count;
    var actualStart = start ?? (step > 0 ? 0 : len - 1);
    var actualStop = stop ?? (step > 0 ? len : -1);

    if (actualStart < 0) {
      actualStart = Math.Max(0, len + actualStart);
    }

    if (actualStop < 0) {
      actualStop = Math.Max(-1, len + actualStop);
    }

    if (actualStart >= len) {
      actualStart = step > 0 ? len : len - 1;
    }

    if (actualStop > len) {
      actualStop = len;
    }

    var result = new List<Value>();

    if (step > 0) {
      for (var i = actualStart; i < actualStop; i += step) {
        if (i >= 0 && i < len) {
          result.Add(list[i]);
        }
      }
    } else {
      for (var i = actualStart; i > actualStop; i += step) {
        if (i >= 0 && i < len) {
          result.Add(list[i]);
        }
      }
    }

    return result;
  }

  private Value EvaluateFunctionCall(CallExpr call) {
    // Check if it's a macro call or super()
    if (call.Callee is VarExpr varExpr) {
      // Handle super() - render parent block
      if (varExpr.Name == "super") {
        var currentBlock = state.CurrentBlock;
        if (currentBlock != null && state.TryGetParentBlock(currentBlock, out var parentBody)) {
          var sb = new StringBuilder();
          this.EvaluateStatements(parentBody!, sb);
          return Value.FromSafeString(sb.ToString());
        }
        return Value.FromString("");
      }

      if (state.TryGetMacro(varExpr.Name, out var macro)) {
        return this.CallMacro(macro!, call.Args.Select(this.EvaluateExpr).ToList(),
            call.Kwargs.ToDictionary(kv => kv.Key, kv => this.EvaluateExpr(kv.Value)));
      }
    }

    var callee = this.EvaluateExpr(call.Callee);
    var args = call.Args.Select(this.EvaluateExpr).ToList();
    var kwargs = call.Kwargs.ToDictionary(kv => kv.Key, kv => this.EvaluateExpr(kv.Value));

    if (callee.IsCallable()) {
      return callee.Call(args, kwargs, state);
    }

    throw new TemplateError($"'{call.Callee}' is not callable");
  }

  public Value CallMacro(MacroStmt macro, List<Value> args, Dictionary<string, Value> kwargs) {
    state.PushScope();

    // Set up arguments
    for (var i = 0; i < macro.Args.Count; i++) {
      var (name, def) = macro.Args[i];

      if (kwargs.TryGetValue(name, out var kwValue)) {
        state.Set(name, kwValue);
      } else if (i < args.Count) {
        state.Set(name, args[i]);
      } else if (def != null) {
        state.Set(name, EvaluateExpr(def));
      } else {
        state.Set(name, Value.FromNone());
      }
    }

    // Set varargs and kwargs if needed
    var varargs = args.Skip(macro.Args.Count).ToList();
    state.Set("varargs", Value.FromSeq(varargs));
    state.Set("kwargs", Value.FromMap(kwargs.Where(kv => !macro.Args.Any(a => a.Name == kv.Key))
        .ToDictionary(kv => kv.Key, kv => kv.Value)));

    var sb = new StringBuilder();
    this.EvaluateStatements(macro.Body, sb);

    state.PopScope();

    return Value.FromSafeString(sb.ToString());
  }

  private Value EvaluateFilter(FilterExpr filter) {
    var value = this.EvaluateExpr(filter.Expr);
    var args = filter.Args.Select(this.EvaluateExpr).ToList();
    var kwargs = filter.Kwargs.ToDictionary(kv => kv.Key, kv => this.EvaluateExpr(kv.Value));

    if (BuiltinFilters.Filters.TryGetValue(filter.Name, out var builtinFilter)) {
      return builtinFilter(value, args, kwargs, state);
    }

    if (state.Environment.TryGetFilter(filter.Name, out var customFilter)) {
      return customFilter(value, args, kwargs, state);
    }

    throw new TemplateError($"Unknown filter: {filter.Name}");
  }

  private Value EvaluateTest(TestExpr test) {
    var value = this.EvaluateExpr(test.Expr);
    var args = test.Args.Select(this.EvaluateExpr).ToList();

    var result = BuiltinTests.RunTest(value, test.Name, args, state);

    if (test.Negated) {
      return Value.FromBool(!result);
    }

    return Value.FromBool(result);
  }

  private Value EvaluateConditional(ConditionalExpr cond) {
    var condition = this.EvaluateExpr(cond.Condition);
    if (condition.IsTrue) {
      return this.EvaluateExpr(cond.TrueExpr);
    }

    return this.EvaluateExpr(cond.FalseExpr);
  }

  private Value EvaluateList(ListExpr list) {
    var items = list.Items.Select(this.EvaluateExpr).ToList();
    return Value.FromSeq(items);
  }

  private Value EvaluateDict(DictExpr dict) {
    var items = new Dictionary<string, Value>();
    foreach (var (keyExpr, valueExpr) in dict.Items) {
      var key = this.EvaluateExpr(keyExpr).AsString();
      var value = this.EvaluateExpr(valueExpr);
      items[key] = value;
    }

    return Value.FromMap(items);
  }
}

/// <summary>
/// Loop helper object.
/// </summary>
public class LoopObject(int index, int length, ForLoopStmt? recursiveLoop = null, Evaluator? evaluator = null, StringBuilder? sb = null) : IObject {
  public int Index0 => index;
  public int Index => index + 1;
  public int RevIndex0 => length - index - 1;
  public int RevIndex => length - index;
  public bool First => index == 0;
  public bool Last => index == length - 1;
  public int LoopLength => length;
  public int Depth => 1;
  public int Depth0 => 0;
  public Value PrevItem => Value.FromNone();
  public Value NextItem => Value.FromNone();

  public Value Cycle(List<Value> items) {
    if (items.Count == 0) {
      return Value.FromNone();
    }

    return items[index % items.Count];
  }

  public bool Changed(Value val) => true;

  public bool TryGetAttr(string name, out Value value) {
    value = name switch {
      "index0" => Value.FromInt(this.Index0),
      "index" => Value.FromInt(this.Index),
      "revindex0" => Value.FromInt(this.RevIndex0),
      "revindex" => Value.FromInt(this.RevIndex),
      "first" => Value.FromBool(this.First),
      "last" => Value.FromBool(this.Last),
      "length" => Value.FromInt(this.LoopLength),
      "depth" => Value.FromInt(this.Depth),
      "depth0" => Value.FromInt(this.Depth0),
      "previtem" => this.PrevItem,
      "nextitem" => this.NextItem,
      "cycle" => Value.FromCallable((args, _, _) => this.Cycle(args)),
      "changed" => Value.FromCallable((args, _, _) => Value.FromBool(args.Count > 0 && this.Changed(args[0]))),
      _ => Value.FromNone()
    };
    return name is "index0" or "index" or "revindex0" or "revindex" or "first" or "last"
        or "length" or "depth" or "depth0" or "previtem" or "nextitem" or "cycle" or "changed";
  }

  public bool TryGetItem(Value key, out Value value) {
    return this.TryGetAttr(key.AsString(), out value);
  }

  public IEnumerable<Value>? TryIter() => null;
  public int? Length => null;

  public Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state) {
    // Recursive loop call: loop(items)
    if (recursiveLoop == null || evaluator == null || sb == null) {
      return null;
    }

    if (args.Count == 0) {
      return Value.FromString("");
    }

    var newItems = evaluator.GetIterableItems(args[0]);
    if (newItems.Count == 0) {
      return Value.FromString("");
    }

    var recursiveSb = new StringBuilder();
    for (var i = 0; i < newItems.Count; i++) {
      evaluator.State.PushScope();

      var item = newItems[i];
      evaluator.SetLoopVar(recursiveLoop.Target, recursiveLoop.Target2, item);

      // Set new loop object for this recursive iteration
      var loopObj = new LoopObject(i, newItems.Count, recursiveLoop, evaluator, recursiveSb);
      evaluator.State.Set("loop", Value.FromObject(loopObj));

      evaluator.EvaluateStatements(recursiveLoop.Body, recursiveSb);

      evaluator.State.PopScope();
    }

    return Value.FromSafeString(recursiveSb.ToString());
  }
}

/// <summary>
/// Imported module wrapper.
/// </summary>
public class ImportedModule(State state) : IObject {
  public bool TryGetAttr(string name, out Value value) {
    if (state.TryGetMacro(name, out var macro)) {
      value = Value.FromCallable((args, kwargs, callingState) => {
        // Use the imported module's state for macro lookup, but pass values from the calling state
        var evaluator = new Evaluator(state);
        return evaluator.CallMacro(macro!, args, kwargs);
      });
      return true;
    }

    return state.TryGet(name, out value);
  }

  public bool TryGetItem(Value key, out Value value) {
    return this.TryGetAttr(key.AsString(), out value);
  }

  public IEnumerable<Value>? TryIter() => null;
  public int? Length => null;
  public Value? Call(List<Value> args, Dictionary<string, Value> kwargs, State state) => null;
}
