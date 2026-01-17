namespace MiniJinja;

/// <summary>
/// Token types for the lexer.
/// </summary>
public enum TokenType {
  TemplateData,
  VariableStart,
  VariableEnd,
  BlockStart,
  BlockEnd,
  Ident,
  String,
  Integer,
  Float,
  Plus,
  Minus,
  Star,
  Slash,
  DoubleSlash,
  Percent,
  DoubleStar,
  Eq,
  Ne,
  Lt,
  Le,
  Gt,
  Ge,
  Assign,
  Pipe,
  Dot,
  Comma,
  Colon,
  Tilde,
  ParenOpen,
  ParenClose,
  BracketOpen,
  BracketClose,
  BraceOpen,
  BraceClose,

  // Keywords
  True,
  False,
  None,
  And,
  Or,
  Not,
  Is,
  In,
  If,
  Elif,
  Else,
  EndIf,
  For,
  EndFor,
  Set,
  Block,
  EndBlock,
  Extends,
  Include,
  Macro,
  EndMacro,
  Call,
  EndCall,
  With,
  EndWith,
  Filter,
  EndFilter,
  Import,
  From,
  Raw,
  EndRaw,
  Autoescape,
  EndAutoescape,

  Eof
}

/// <summary>
/// A token from the lexer.
/// </summary>
public readonly record struct Token(TokenType Type, string Value, int Line, int Column);

/// <summary>
/// Lexer for Jinja2-style templates.
/// </summary>
public class Lexer(string source) {
  private static readonly Dictionary<string, TokenType> Keywords = new() {
    ["true"] = TokenType.True,
    ["false"] = TokenType.False,
    ["none"] = TokenType.None,
    ["and"] = TokenType.And,
    ["or"] = TokenType.Or,
    ["not"] = TokenType.Not,
    ["is"] = TokenType.Is,
    ["in"] = TokenType.In,
    ["if"] = TokenType.If,
    ["elif"] = TokenType.Elif,
    ["else"] = TokenType.Else,
    ["endif"] = TokenType.EndIf,
    ["for"] = TokenType.For,
    ["endfor"] = TokenType.EndFor,
    ["set"] = TokenType.Set,
    ["block"] = TokenType.Block,
    ["endblock"] = TokenType.EndBlock,
    ["extends"] = TokenType.Extends,
    ["include"] = TokenType.Include,
    ["macro"] = TokenType.Macro,
    ["endmacro"] = TokenType.EndMacro,
    ["call"] = TokenType.Call,
    ["endcall"] = TokenType.EndCall,
    ["with"] = TokenType.With,
    ["endwith"] = TokenType.EndWith,
    ["filter"] = TokenType.Filter,
    ["endfilter"] = TokenType.EndFilter,
    ["import"] = TokenType.Import,
    ["from"] = TokenType.From,
    ["raw"] = TokenType.Raw,
    ["endraw"] = TokenType.EndRaw,
    ["autoescape"] = TokenType.Autoescape,
    ["endautoescape"] = TokenType.EndAutoescape,
  };
  private int pos;
  private int line = 1;
  private int column = 0;
  private bool inExpression;

  private const string BlockStart = "{%";
  private const string BlockEnd = "%}";
  private const string VarStart = "{{";
  private const string VarEnd = "}}";
  private const string CommentStart = "{#";
  private const string CommentEnd = "#}";

  public List<Token> Tokenize() {
    var tokens = new List<Token>();

    while (pos < source.Length) {
      if (!inExpression) {
        // Look for special markers
        if (this.Match(CommentStart)) {
          // Skip comment
          this.SkipUntil(CommentEnd);
          continue;
        }

        if (this.Match(VarStart)) {
          tokens.Add(new Token(TokenType.VariableStart, VarStart, line, column));
          pos += 2;
          column += 2;
          // Handle whitespace control: {{- must have - immediately after {{
          if (this.Peek() == '-') {
            pos++;
            column++;
          }
          this.SkipWhitespace();
          inExpression = true;
          continue;
        }

        if (this.Match(BlockStart)) {
          tokens.Add(new Token(TokenType.BlockStart, BlockStart, line, column));
          pos += 2;
          column += 2;
          // Handle whitespace control: {%- must have - immediately after {%
          if (this.Peek() == '-') {
            pos++;
            column++;
          }
          this.SkipWhitespace();
          inExpression = true;
          continue;
        }

        // Read template data
        var start = pos;
        while (pos < source.Length && !this.Match(VarStart) && !this.Match(BlockStart) && !this.Match(CommentStart)) {
          this.Advance();
        }

        if (pos > start) {
          tokens.Add(new Token(TokenType.TemplateData, source[start..pos], line, column));
        }
      } else {
        this.SkipWhitespace();

        // Handle whitespace control before end markers
        if (this.Peek() == '-' && (this.Match("-}}") || this.Match("-%}"))) {
          pos++;
          column++;
          this.SkipWhitespace();
        }

        if (this.Match(VarEnd)) {
          tokens.Add(new Token(TokenType.VariableEnd, VarEnd, line, column));
          pos += 2;
          column += 2;
          inExpression = false;
          continue;
        }

        if (this.Match(BlockEnd)) {
          tokens.Add(new Token(TokenType.BlockEnd, BlockEnd, line, column));
          pos += 2;
          column += 2;
          inExpression = false;
          continue;
        }

        var token = this.ReadExpressionToken();
        if (token.HasValue) {
          tokens.Add(token.Value);
        }
      }
    }

    tokens.Add(new Token(TokenType.Eof, "", line, column));
    return tokens;
  }

  private Token? ReadExpressionToken() {
    this.SkipWhitespace();
    if (pos >= source.Length) {
      return null;
    }

    var ch = this.Peek();

    // String literals
    if (ch == '"' || ch == '\'') {
      return this.ReadString(ch);
    }

    // Numbers
    if (char.IsDigit(ch)) {
      return this.ReadNumber();
    }

    // Identifiers and keywords
    if (char.IsLetter(ch) || ch == '_') {
      return this.ReadIdent();
    }

    // Operators and punctuation
    return this.ReadOperator();
  }

  private Token ReadString(char quote) {
    var startLine = line;
    var startCol = column;
    pos++; // skip opening quote
    column++;

    var sb = new System.Text.StringBuilder();
    while (pos < source.Length && this.Peek() != quote) {
      var ch = this.Advance();
      if (ch == '\\' && pos < source.Length) {
        var escaped = this.Advance();
        sb.Append(escaped switch {
          'n' => '\n',
          'r' => '\r',
          't' => '\t',
          '\\' => '\\',
          '"' => '"',
          '\'' => '\'',
          _ => escaped
        });
      } else {
        sb.Append(ch);
      }
    }

    if (pos < source.Length) {
      pos++; // skip closing quote
      column++;
    }

    return new Token(TokenType.String, sb.ToString(), startLine, startCol);
  }

  private Token ReadNumber() {
    var startLine = line;
    var startCol = column;
    var start = pos;
    var isFloat = false;

    while (pos < source.Length && char.IsDigit(this.Peek())) {
      this.Advance();
    }

    if (pos < source.Length && this.Peek() == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])) {
      isFloat = true;
      this.Advance(); // consume .
      while (pos < source.Length && char.IsDigit(this.Peek())) {
        this.Advance();
      }
    }

    if (pos < source.Length && (this.Peek() == 'e' || this.Peek() == 'E')) {
      isFloat = true;
      this.Advance();
      if (pos < source.Length && (this.Peek() == '+' || this.Peek() == '-')) {
        this.Advance();
      }

      while (pos < source.Length && char.IsDigit(this.Peek())) {
        this.Advance();
      }
    }

    var value = source[start..pos];
    return new Token(isFloat ? TokenType.Float : TokenType.Integer, value, startLine, startCol);
  }

  private Token ReadIdent() {
    var startLine = line;
    var startCol = column;
    var start = pos;

    while (pos < source.Length && (char.IsLetterOrDigit(this.Peek()) || this.Peek() == '_')) {
      this.Advance();
    }

    var value = source[start..pos];

    // Check if it's a keyword
    if (Keywords.TryGetValue(value.ToLowerInvariant(), out var kwType)) {
      return new Token(kwType, value, startLine, startCol);
    }

    return new Token(TokenType.Ident, value, startLine, startCol);
  }

  private Token? ReadOperator() {
    var startLine = line;
    var startCol = column;

    // Two-character operators
    if (pos + 1 < source.Length) {
      var two = source.Substring(pos, 2);
      var type = two switch {
        "==" => TokenType.Eq,
        "!=" => TokenType.Ne,
        "<=" => TokenType.Le,
        ">=" => TokenType.Ge,
        "//" => TokenType.DoubleSlash,
        "**" => TokenType.DoubleStar,
        _ => (TokenType?)null
      };
      if (type.HasValue) {
        pos += 2;
        column += 2;
        return new Token(type.Value, two, startLine, startCol);
      }
    }

    // Single-character operators
    var ch = this.Advance();
    var tokenType = ch switch {
      '+' => TokenType.Plus,
      '-' => TokenType.Minus,
      '*' => TokenType.Star,
      '/' => TokenType.Slash,
      '%' => TokenType.Percent,
      '=' => TokenType.Assign,
      '<' => TokenType.Lt,
      '>' => TokenType.Gt,
      '|' => TokenType.Pipe,
      '.' => TokenType.Dot,
      ',' => TokenType.Comma,
      ':' => TokenType.Colon,
      '~' => TokenType.Tilde,
      '(' => TokenType.ParenOpen,
      ')' => TokenType.ParenClose,
      '[' => TokenType.BracketOpen,
      ']' => TokenType.BracketClose,
      '{' => TokenType.BraceOpen,
      '}' => TokenType.BraceClose,
      _ => (TokenType?)null
    };

    if (tokenType.HasValue) {
      return new Token(tokenType.Value, ch.ToString(), startLine, startCol);
    }

    return null;
  }

  private bool Match(string s) {
    if (this.pos + s.Length > source.Length) {
      return false;
    }

    return source.Substring(pos, s.Length) == s;
  }

  private char Peek() => pos < source.Length ? source[pos] : '\0';

  private char Advance() {
    if (pos >= source.Length) {
      return '\0';
    }

    var ch = source[pos++];
    if (ch == '\n') {
      line++;
      column = 0;
    } else {
      column++;
    }

    return ch;
  }

  private void SkipWhitespace() {
    while (pos < source.Length && char.IsWhiteSpace(this.Peek())) {
      this.Advance();
    }
  }

  private void SkipUntil(string marker) {
    while (pos < source.Length && !this.Match(marker)) {
      this.Advance();
    }

    if (this.Match(marker)) {
      pos += marker.Length;
      column += marker.Length;
    }
  }
}
