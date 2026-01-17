namespace MiniJinja;

/// <summary>
/// Parser for Jinja2 templates.
/// </summary>
public class Parser(IEnumerable<Token> tokens) {
  private readonly List<Token> tokens = tokens.ToList();
  private int pos = 0;

  private Token Current => pos < tokens.Count ? tokens[pos] : new Token(TokenType.Eof, "", 0, 0);
  private Token? Peek(int offset = 1) => pos + offset < tokens.Count ? tokens[pos + offset] : null;

  private Token Advance() {
    var token = Current;
    pos++;
    return token;
  }

  private bool Check(TokenType type) => Current.Type == type;
  private bool Check(params TokenType[] types) => types.Contains(Current.Type);

  private Token Expect(TokenType type, string message) {
    if (Current.Type != type)
      throw new TemplateError($"Expected {type}, got {Current.Type}: {message}");
    return Advance();
  }

  private bool Match(TokenType type) {
    if (Check(type)) {
      Advance();
      return true;
    }
    return false;
  }

  public TemplateNode Parse() {
    var body = ParseStatements();
    return new TemplateNode(body);
  }

  private List<Stmt> ParseStatements(params TokenType[] endTokens) {
    var statements = new List<Stmt>();

    while (!Check(TokenType.Eof)) {
      // Check if we're at an end token block
      if (Check(TokenType.BlockStart) && IsEndBlock(endTokens)) {
        break;
      }
      statements.Add(ParseStatement());
    }

    return statements;
  }

  private bool IsEndBlock(TokenType[] endTokens) {
    // Look ahead to see if this block starts with one of the end tokens
    var i = 1;
    var next = Peek(i);

    // Skip whitespace control minus
    if (next?.Type == TokenType.Minus) {
      i++;
      next = Peek(i);
    }

    if (next == null) return false;
    return endTokens.Contains(next.Value.Type);
  }

  private Stmt ParseStatement() {
    return Current.Type switch {
      TokenType.TemplateData => ParseTemplateData(),
      TokenType.VariableStart => ParseEmitExpr(),
      TokenType.BlockStart => ParseBlockStatement(),
      _ => throw new TemplateError($"Unexpected token: {Current.Type}")
    };
  }

  private Stmt ParseTemplateData() {
    var token = Expect(TokenType.TemplateData, "expected template data");
    return new TemplateDataStmt(token.Value);
  }

  private Stmt ParseEmitExpr() {
    Expect(TokenType.VariableStart, "expected {{");
    var expr = ParseExpr();
    Expect(TokenType.VariableEnd, "expected }}");
    return new EmitExprStmt(expr);
  }

  private Stmt ParseBlockStatement() {
    Expect(TokenType.BlockStart, "expected {%");

    // Skip whitespace control
    Match(TokenType.Minus);

    var stmt = Current.Type switch {
      TokenType.For => ParseForLoop(),
      TokenType.EndFor => ParseEndTag(TokenType.EndFor),
      TokenType.If => ParseIf(),
      TokenType.Elif => ParseEndTag(TokenType.Elif),
      TokenType.Else => ParseEndTag(TokenType.Else),
      TokenType.EndIf => ParseEndTag(TokenType.EndIf),
      TokenType.Set => ParseSet(),
      TokenType.Block => ParseBlock(),
      TokenType.EndBlock => ParseEndTag(TokenType.EndBlock),
      TokenType.Extends => ParseExtends(),
      TokenType.Include => ParseInclude(),
      TokenType.Macro => ParseMacro(),
      TokenType.EndMacro => ParseEndTag(TokenType.EndMacro),
      TokenType.Call => ParseCallBlock(),
      TokenType.EndCall => ParseEndTag(TokenType.EndCall),
      TokenType.With => ParseWith(),
      TokenType.EndWith => ParseEndTag(TokenType.EndWith),
      TokenType.Filter => ParseFilterBlock(),
      TokenType.EndFilter => ParseEndTag(TokenType.EndFilter),
      TokenType.Import => ParseImport(),
      TokenType.From => ParseFromImport(),
      TokenType.Raw => ParseRaw(),
      TokenType.EndRaw => ParseEndTag(TokenType.EndRaw),
      TokenType.Autoescape => ParseAutoescape(),
      TokenType.EndAutoescape => ParseEndTag(TokenType.EndAutoescape),
      _ => throw new TemplateError($"Unknown block statement: {Current.Type} ({Current.Value})")
    };

    return stmt;
  }

  private Stmt ParseEndTag(TokenType type) {
    Advance(); // consume the end tag keyword
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");
    return new EndTagMarker(type);
  }

  private Stmt ParseForLoop() {
    Advance(); // consume 'for'

    var target = Expect(TokenType.Ident, "expected variable name").Value;
    string? target2 = null;

    if (Match(TokenType.Comma)) {
      target2 = Expect(TokenType.Ident, "expected second variable name").Value;
    }

    Expect(TokenType.In, "expected 'in'");
    var iter = ParseExprNoIf();

    Expr? filter = null;
    if (Match(TokenType.If)) {
      filter = ParseExpr();
    }

    bool recursive = Match(TokenType.Ident) && tokens[pos - 1].Value == "recursive";

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndFor, TokenType.Else);
    var elseBody = new List<Stmt>();

    if (Current.Type == TokenType.BlockStart) {
      var nextToken = Peek();
      if (nextToken?.Type == TokenType.Else || (nextToken?.Type == TokenType.Minus && Peek(2)?.Type == TokenType.Else)) {
        Advance(); // consume {%
        Match(TokenType.Minus);
        Advance(); // consume else
        Match(TokenType.Minus);
        Expect(TokenType.BlockEnd, "expected %}");
        elseBody = ParseStatements(TokenType.EndFor);
      }
    }

    // Consume endfor
    ExpectEndTag(TokenType.EndFor);

    return new ForLoopStmt(target, target2, iter, filter, body, elseBody, recursive);
  }

  private Stmt ParseIf() {
    Advance(); // consume 'if'
    var condition = ParseExpr();
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var trueBody = ParseStatements(TokenType.EndIf, TokenType.Elif, TokenType.Else);
    var falseBody = ParseElifOrElse();

    ExpectEndTag(TokenType.EndIf);
    return new IfStmt(condition, trueBody, falseBody);
  }

  private List<Stmt> ParseElifOrElse() {
    if (Current.Type != TokenType.BlockStart)
      return [];

    var nextToken = Peek();
    var skipMinus = nextToken?.Type == TokenType.Minus;
    var actualNextToken = skipMinus ? Peek(2) : nextToken;

    if (actualNextToken?.Type == TokenType.Elif) {
      Advance(); // consume {%
      Match(TokenType.Minus);
      Advance(); // consume elif
      var elifCondition = ParseExpr();
      Match(TokenType.Minus);
      Expect(TokenType.BlockEnd, "expected %}");

      var elifBody = ParseStatements(TokenType.EndIf, TokenType.Elif, TokenType.Else);
      var elifFalse = ParseElifOrElse(); // Recursively handle next elif/else
      return [new IfStmt(elifCondition, elifBody, elifFalse)];
    } else if (actualNextToken?.Type == TokenType.Else) {
      Advance(); // consume {%
      Match(TokenType.Minus);
      Advance(); // consume else
      Match(TokenType.Minus);
      Expect(TokenType.BlockEnd, "expected %}");
      return ParseStatements(TokenType.EndIf);
    }

    return [];
  }

  private Stmt ParseSet() {
    Advance(); // consume 'set'
    var name = Expect(TokenType.Ident, "expected variable name").Value;

    string? attr = null;
    if (Match(TokenType.Dot)) {
      attr = Expect(TokenType.Ident, "expected attribute name").Value;
    }

    Expect(TokenType.Assign, "expected '='");
    var value = ParseExpr();
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    return new SetStmt(name, attr, value);
  }

  private Stmt ParseBlock() {
    Advance(); // consume 'block'
    var name = Expect(TokenType.Ident, "expected block name").Value;
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndBlock);
    ExpectEndTag(TokenType.EndBlock);

    return new BlockStmt(name, body);
  }

  private Stmt ParseExtends() {
    Advance(); // consume 'extends'
    var name = ParseExpr();
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    return new ExtendsStmt(name);
  }

  private Stmt ParseInclude() {
    Advance(); // consume 'include'
    var name = ParseExpr();

    bool ignoreMissing = false;
    if (Check(TokenType.Ident) && Current.Value == "ignore") {
      Advance();
      if (Check(TokenType.Ident) && Current.Value == "missing") {
        Advance();
        ignoreMissing = true;
      }
    }

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    return new IncludeStmt(name, ignoreMissing);
  }

  private Stmt ParseMacro() {
    Advance(); // consume 'macro'
    var name = Expect(TokenType.Ident, "expected macro name").Value;
    var args = ParseArgumentList();
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndMacro);
    ExpectEndTag(TokenType.EndMacro);

    return new MacroStmt(name, args, body);
  }

  private Stmt ParseCallBlock() {
    Advance(); // consume 'call'

    var args = new List<(string Name, Expr? Default)>();
    if (Check(TokenType.ParenOpen)) {
      args = ParseArgumentList();
    }

    var callExpr = ParseExpr();
    if (callExpr is not CallExpr call) {
      // Wrap in a call if it's just an identifier
      if (callExpr is VarExpr varExpr) {
        call = new CallExpr(varExpr, [], []);
      } else {
        throw new TemplateError("Expected a call expression in call block");
      }
    } else {
      call = (CallExpr)callExpr;
    }

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndCall);
    ExpectEndTag(TokenType.EndCall);

    return new CallBlockStmt(call, args, body);
  }

  private Stmt ParseWith() {
    Advance(); // consume 'with'

    var bindings = new List<(string Name, Expr Value)>();

    do {
      var name = Expect(TokenType.Ident, "expected variable name").Value;
      Expect(TokenType.Assign, "expected '='");
      var value = ParseExpr();
      bindings.Add((name, value));
    } while (Match(TokenType.Comma));

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndWith);
    ExpectEndTag(TokenType.EndWith);

    return new WithStmt(bindings, body);
  }

  private Stmt ParseFilterBlock() {
    Advance(); // consume 'filter'
    var name = Expect(TokenType.Ident, "expected filter name").Value;

    var args = new List<Expr>();
    if (Match(TokenType.ParenOpen)) {
      if (!Check(TokenType.ParenClose)) {
        do {
          args.Add(ParseExpr());
        } while (Match(TokenType.Comma));
      }
      Expect(TokenType.ParenClose, "expected ')'");
    }

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndFilter);
    ExpectEndTag(TokenType.EndFilter);

    return new FilterBlockStmt(name, args, body);
  }

  private Stmt ParseImport() {
    Advance(); // consume 'import'
    var template = ParseExpr();
    Expect(TokenType.Ident, "expected 'as'");
    var asName = Expect(TokenType.Ident, "expected name").Value;
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    return new ImportStmt(template, asName);
  }

  private Stmt ParseFromImport() {
    Advance(); // consume 'from'
    var template = ParseExpr();
    Expect(TokenType.Import, "expected 'import'");

    var names = new List<(string Name, string? Alias)>();
    do {
      var name = Expect(TokenType.Ident, "expected name").Value;
      string? alias = null;
      if (Check(TokenType.Ident) && Current.Value == "as") {
        Advance();
        alias = Expect(TokenType.Ident, "expected alias").Value;
      }
      names.Add((name, alias));
    } while (Match(TokenType.Comma));

    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    return new FromImportStmt(template, names);
  }

  private Stmt ParseRaw() {
    Advance(); // consume 'raw'
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    // Collect raw content until endraw
    var content = new System.Text.StringBuilder();
    while (!Check(TokenType.Eof)) {
      if (Current.Type == TokenType.BlockStart) {
        var next = Peek();
        var skipMinus = next?.Type == TokenType.Minus;
        var actual = skipMinus ? Peek(2) : next;
        if (actual?.Type == TokenType.EndRaw) {
          break;
        }
      }
      content.Append(Current.Value);
      Advance();
    }

    ExpectEndTag(TokenType.EndRaw);
    return new TemplateDataStmt(content.ToString());
  }

  private Stmt ParseAutoescape() {
    Advance(); // consume 'autoescape'
    ParseExpr(); // consume the autoescape mode expression
    Match(TokenType.Minus);
    Expect(TokenType.BlockEnd, "expected %}");

    var body = ParseStatements(TokenType.EndAutoescape);
    ExpectEndTag(TokenType.EndAutoescape);

    // For now, just return the body statements wrapped
    return new AutoescapeStmt(body);
  }

  private void ExpectEndTag(TokenType endType) {
    if (Current.Type == TokenType.BlockStart) {
      Advance();
      Match(TokenType.Minus);
      if (!Check(endType)) {
        throw new TemplateError($"Expected {endType}, got {Current.Type}");
      }
      Advance();
      Match(TokenType.Minus);
      Expect(TokenType.BlockEnd, "expected %}");
    }
  }

  private List<(string Name, Expr? Default)> ParseArgumentList() {
    var args = new List<(string Name, Expr? Default)>();

    Expect(TokenType.ParenOpen, "expected '('");

    if (!Check(TokenType.ParenClose)) {
      do {
        var name = Expect(TokenType.Ident, "expected parameter name").Value;
        Expr? defaultValue = null;
        if (Match(TokenType.Assign)) {
          defaultValue = ParseExpr();
        }
        args.Add((name, defaultValue));
      } while (Match(TokenType.Comma));
    }

    Expect(TokenType.ParenClose, "expected ')'");
    return args;
  }

  // Expression parsing with precedence climbing
  public Expr ParseExpr() => ParseConditional();

  /// <summary>
  /// Parse an expression without handling the inline if (ternary) expression.
  /// Used in for loops where 'if' is used for filtering.
  /// </summary>
  private Expr ParseExprNoIf() => ParseOr();

  private Expr ParseConditional() {
    var expr = ParseOr();

    if (Match(TokenType.If)) {
      var condition = ParseOr();
      Expr falseExpr = new LiteralExpr(Value.FromNone());
      if (Match(TokenType.Else)) {
        falseExpr = ParseConditional();
      }
      return new ConditionalExpr(condition, expr, falseExpr);
    }

    return expr;
  }

  private Expr ParseOr() {
    var left = ParseAnd();

    while (Match(TokenType.Or)) {
      var right = ParseAnd();
      left = new BinaryExpr("or", left, right);
    }

    return left;
  }

  private Expr ParseAnd() {
    var left = ParseNot();

    while (Match(TokenType.And)) {
      var right = ParseNot();
      left = new BinaryExpr("and", left, right);
    }

    return left;
  }

  private Expr ParseNot() {
    if (Match(TokenType.Not)) {
      var expr = ParseNot();
      return new UnaryExpr("not", expr);
    }

    return ParseComparison();
  }

  private Expr ParseComparison() {
    var left = ParseConcat();

    while (Check(TokenType.Eq, TokenType.Ne, TokenType.Lt, TokenType.Le, TokenType.Gt, TokenType.Ge, TokenType.In)) {
      string op;
      bool notIn = false;

      if (Check(TokenType.Not)) {
        Advance();
        Expect(TokenType.In, "expected 'in' after 'not'");
        op = "not in";
        notIn = true;
      } else {
        op = Current.Type switch {
          TokenType.Eq => "==",
          TokenType.Ne => "!=",
          TokenType.Lt => "<",
          TokenType.Le => "<=",
          TokenType.Gt => ">",
          TokenType.Ge => ">=",
          TokenType.In => "in",
          _ => throw new TemplateError($"Unknown comparison operator: {Current.Type}")
        };
        if (!notIn) Advance();
      }

      var right = ParseConcat();
      left = new BinaryExpr(op, left, right);
    }

    // Handle "is" and "is not" tests
    if (Check(TokenType.Is)) {
      Advance();
      bool negated = Match(TokenType.Not);
      var testName = Expect(TokenType.Ident, "expected test name").Value;

      var args = new List<Expr>();
      if (Match(TokenType.ParenOpen)) {
        if (!Check(TokenType.ParenClose)) {
          do {
            args.Add(ParseExpr());
          } while (Match(TokenType.Comma));
        }
        Expect(TokenType.ParenClose, "expected ')'");
      }

      left = new TestExpr(left, testName, args, negated);
    }

    return left;
  }

  private Expr ParseConcat() {
    var left = ParseAdditive();

    while (Match(TokenType.Tilde)) {
      var right = ParseAdditive();
      left = new BinaryExpr("~", left, right);
    }

    return left;
  }

  private Expr ParseAdditive() {
    var left = ParseMultiplicative();

    while (Check(TokenType.Plus, TokenType.Minus)) {
      var op = Current.Type == TokenType.Plus ? "+" : "-";
      Advance();
      var right = ParseMultiplicative();
      left = new BinaryExpr(op, left, right);
    }

    return left;
  }

  private Expr ParseMultiplicative() {
    var left = ParsePower();

    while (Check(TokenType.Star, TokenType.Slash, TokenType.DoubleSlash, TokenType.Percent)) {
      var op = Current.Type switch {
        TokenType.Star => "*",
        TokenType.Slash => "/",
        TokenType.DoubleSlash => "//",
        TokenType.Percent => "%",
        _ => throw new TemplateError("Unexpected operator")
      };
      Advance();
      var right = ParsePower();
      left = new BinaryExpr(op, left, right);
    }

    return left;
  }

  private Expr ParsePower() {
    var left = ParseUnary();

    while (Match(TokenType.DoubleStar)) {
      var right = ParseUnary();
      left = new BinaryExpr("**", left, right);
    }

    return left;
  }

  private Expr ParseUnary() {
    // First parse unary operators and primary
    var expr = ParseUnaryOnly();

    // Then postfix operations (., [], ())
    expr = ParsePostfix(expr);

    // Then filters (|)
    return ParseFilters(expr);
  }
  private Expr ParseUnaryOnly() {
    if (Check(TokenType.Minus, TokenType.Plus)) {
      var op = Current.Type == TokenType.Minus ? "-" : "+";
      Advance();
      var inner = ParseUnaryOnly();
      return new UnaryExpr(op, inner);
    }

    return ParsePrimary();
  }

  private Expr ParsePostfix(Expr expr) {
    while (true) {
      if (Match(TokenType.Dot)) {
        var attr = Expect(TokenType.Ident, "expected attribute name").Value;
        expr = new GetAttrExpr(expr, attr);
      } else if (Match(TokenType.BracketOpen)) {
        // Could be index or slice
        Expr? start = null;
        Expr? stop = null;
        Expr? step = null;
        bool isSlice = false;

        if (!Check(TokenType.Colon)) {
          start = ParseExpr();
        }

        if (Match(TokenType.Colon)) {
          isSlice = true;
          if (!Check(TokenType.Colon, TokenType.BracketClose)) {
            stop = ParseExpr();
          }
          if (Match(TokenType.Colon)) {
            if (!Check(TokenType.BracketClose)) {
              step = ParseExpr();
            }
          }
        }

        Expect(TokenType.BracketClose, "expected ']'");

        if (isSlice) {
          expr = new SliceExpr(expr, start, stop, step);
        } else {
          expr = new GetItemExpr(expr, start!);
        }
      } else if (Match(TokenType.ParenOpen)) {
        var args = new List<Expr>();
        var kwargs = new Dictionary<string, Expr>();

        if (!Check(TokenType.ParenClose)) {
          do {
            // Check for keyword argument
            if (Check(TokenType.Ident) && Peek()?.Type == TokenType.Assign) {
              var name = Advance().Value;
              Advance(); // consume =
              var value = ParseExpr();
              kwargs[name] = value;
            } else {
              args.Add(ParseExpr());
            }
          } while (Match(TokenType.Comma));
        }

        Expect(TokenType.ParenClose, "expected ')'");
        expr = new CallExpr(expr, args, kwargs);
      } else {
        break;
      }
    }

    return expr;
  }

  private Expr ParseFilters(Expr expr) {
    while (Match(TokenType.Pipe)) {
      var filterName = Expect(TokenType.Ident, "expected filter name").Value;
      var args = new List<Expr>();
      var kwargs = new Dictionary<string, Expr>();

      if (Match(TokenType.ParenOpen)) {
        if (!Check(TokenType.ParenClose)) {
          do {
            if (Check(TokenType.Ident) && Peek()?.Type == TokenType.Assign) {
              var name = Advance().Value;
              Advance();
              var value = ParseExpr();
              kwargs[name] = value;
            } else {
              args.Add(ParseExpr());
            }
          } while (Match(TokenType.Comma));
        }
        Expect(TokenType.ParenClose, "expected ')'");
      }

      expr = new FilterExpr(expr, filterName, args, kwargs);
    }

    return expr;
  }

  private Expr ParsePrimary() {
    // Literals
    if (Check(TokenType.Integer)) {
      var value = long.Parse(Advance().Value);
      return new LiteralExpr(Value.FromInt(value));
    }

    if (Check(TokenType.Float)) {
      var value = double.Parse(Advance().Value, System.Globalization.CultureInfo.InvariantCulture);
      return new LiteralExpr(Value.FromFloat(value));
    }

    if (Check(TokenType.String)) {
      var value = Advance().Value;
      return new LiteralExpr(Value.FromString(value));
    }

    if (Match(TokenType.True)) {
      return new LiteralExpr(Value.FromBool(true));
    }

    if (Match(TokenType.False)) {
      return new LiteralExpr(Value.FromBool(false));
    }

    if (Match(TokenType.None)) {
      return new LiteralExpr(Value.FromNone());
    }

    // Parenthesized expression or tuple
    if (Match(TokenType.ParenOpen)) {
      if (Check(TokenType.ParenClose)) {
        Advance();
        return new ListExpr([]);
      }

      var first = ParseExpr();

      if (Match(TokenType.Comma)) {
        // Tuple
        var items = new List<Expr> { first };
        if (!Check(TokenType.ParenClose)) {
          do {
            items.Add(ParseExpr());
          } while (Match(TokenType.Comma) && !Check(TokenType.ParenClose));
        }
        Expect(TokenType.ParenClose, "expected ')'");
        return new ListExpr(items);
      }

      Expect(TokenType.ParenClose, "expected ')'");
      return first;
    }

    // List
    if (Match(TokenType.BracketOpen)) {
      var items = new List<Expr>();
      if (!Check(TokenType.BracketClose)) {
        do {
          items.Add(ParseExpr());
        } while (Match(TokenType.Comma) && !Check(TokenType.BracketClose));
      }
      Expect(TokenType.BracketClose, "expected ']'");
      return new ListExpr(items);
    }

    // Dict
    if (Match(TokenType.BraceOpen)) {
      var items = new List<(Expr Key, Expr Value)>();
      if (!Check(TokenType.BraceClose)) {
        do {
          var key = ParseExpr();
          Expect(TokenType.Colon, "expected ':'");
          var value = ParseExpr();
          items.Add((key, value));
        } while (Match(TokenType.Comma) && !Check(TokenType.BraceClose));
      }
      Expect(TokenType.BraceClose, "expected '}'");
      return new DictExpr(items);
    }

    // Identifier
    if (Check(TokenType.Ident)) {
      var name = Advance().Value;
      return new VarExpr(name);
    }

    throw new TemplateError($"Unexpected token in expression: {Current.Type} ({Current.Value})");
  }
}

// Marker for end tags during parsing
internal class EndTagMarker(TokenType type) : Stmt {
  public TokenType Type { get; } = type;
}

// Autoescape wrapper (simplified)
public class AutoescapeStmt(List<Stmt> body) : Stmt {
  public List<Stmt> Body { get; } = body;
}
