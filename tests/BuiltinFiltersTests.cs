namespace MiniJinja.Tests;

using FluentAssertions;
using Xunit;

public class BuiltinFiltersTests {
  [Theory]
  [InlineData("{{ 'hello world'|title }}", "Hello World")]
  [InlineData("{{ 'hello  world'|title }}", "Hello  World")]
  [InlineData("{{ ''|capitalize }}", "")]
  public void TitleAndCapitalize_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'hello'|length }}", "5")]
  [InlineData("{{ {'a':1,'b':2}|length }}", "2")]
  [InlineData("{{ 42|length }}", "0")]
  public void Length_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ []|first }}", "")]
  [InlineData("{{ []|last }}", "")]
  public void FirstLast_Empty_ShouldReturnNone(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'abc'|reverse }}", "cba")]
  [InlineData("{{ [1,2,3]|reverse|join(',') }}", "3,2,1")]
  public void Reverse_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Sort_WithAttribute_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for item in items|sort(attribute='name') %}{{ item.name }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "b" },
        new Dictionary<string, object?> { ["name"] = "a" }
      }
    });
    result.Should().Be("ab");
  }

  [Fact]
  public void Sort_WithReverse_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [1,3,2]|sort(reverse=true)|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("3,2,1");
  }

  [Fact]
  public void Split_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'a-b-c'|split('-')|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("a,b,c");
  }

  [Fact]
  public void Split_DefaultSeparator_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'a b c'|split|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("a,b,c");
  }

  [Theory]
  [InlineData("{{ -3.5|abs }}", "3")]
  [InlineData("{{ 'x'|abs }}", "x")]
  public void Abs_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'xyz'|int }}", "0")]
  [InlineData("{{ 'xyz'|int(99) }}", "99")]
  [InlineData("{{ '42'|int }}", "42")]
  [InlineData("{{ 'xyz'|float }}", "0.0")]
  [InlineData("{{ 'xyz'|float(1.5) }}", "1.5")]
  [InlineData("{{ '3.14'|float }}", "3.14")]
  public void IntFloat_WithDefaults_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ x|d('default') }}", "", "default")]
  [InlineData("{{ x|default('default', boolean=true) }}", "", "default")]
  [InlineData("{{ x|default('default', boolean=true) }}", "value", "value")]
  public void Default_WithBoolean_ShouldWork(string template, string xValue, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var ctx = string.IsNullOrEmpty(xValue)
      ? new Dictionary<string, object?>()
      : new Dictionary<string, object?> { ["x"] = xValue };
    var result = tmpl.Render(ctx);
    result.Should().Be(expected);
  }

  [Fact]
  public void List_FromString_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'abc'|list|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("a,b,c");
  }

  [Fact]
  public void Batch_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for batch in [1,2,3,4,5]|batch(2) %}[{% for i in batch %}{{ i }}{% endfor %}]{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("[12][34][5]");
  }

  [Fact]
  public void Batch_WithFill_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for batch in [1,2,3,4,5]|batch(2, 'x') %}[{% for i in batch %}{{ i }}{% endfor %}]{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("[12][34][5x]");
  }

  [Fact]
  public void Batch_RequiresArg_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [1,2]|batch }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*batch requires*");
  }

  [Fact]
  public void Slice_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for s in [1,2,3,4,5]|slice(3) %}[{% for i in s %}{{ i }}{% endfor %}]{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("[12][34][5]");
  }

  [Fact]
  public void Slice_RequiresArg_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [1,2]|slice }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*slice requires*");
  }

  [Fact]
  public void Items_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for k,v in {'a':1}|items %}{{ k }}={{ v }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("a=1");
  }

  [Fact]
  public void Dictsort_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for k,v in {'b':2,'a':1}|dictsort %}{{ k }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("ab");
  }

  [Fact]
  public void Dictsort_ByValue_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for k,v in {'a':2,'b':1}|dictsort(by='value') %}{{ k }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("ba");
  }

  [Fact]
  public void Dictsort_Reverse_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for k,v in {'a':1,'b':2}|dictsort(reverse=true) %}{{ k }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("ba");
  }

  [Fact]
  public void Dictsort_CaseInsensitive_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for k,v in {'B':2,'a':1}|dictsort(case_sensitive=false) %}{{ k }}{% endfor %}");
    var result = tmpl.Render();
    result.Should().Be("aB");
  }

  [Fact]
  public void Groupby_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for g in items|groupby('type') %}{{ g.grouper }}:{% for i in g.list %}{{ i.name }}{% endfor %};{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["type"] = "a", ["name"] = "1" },
        new Dictionary<string, object?> { ["type"] = "b", ["name"] = "2" },
        new Dictionary<string, object?> { ["type"] = "a", ["name"] = "3" }
      }
    });
    result.Should().Be("a:13;b:2;");
  }

  [Fact]
  public void Groupby_WithAttribute_Kwarg_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for g in items|groupby(attribute='type') %}{{ g.grouper }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["type"] = "x" }
      }
    });
    result.Should().Be("x");
  }

  [Fact]
  public void Groupby_RequiresAttribute_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ []|groupby }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*groupby requires attribute*");
  }

  [Fact]
  public void Map_WithAttribute_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ items|map(attribute='name')|join(',') }}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "a" },
        new Dictionary<string, object?> { ["name"] = "b" }
      }
    });
    result.Should().Be("a,b");
  }

  [Fact]
  public void Map_WithFilter_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ ['a','b']|map('upper')|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("A,B");
  }

  [Fact]
  public void Map_NoArgs_ShouldPassThrough() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [1,2]|map|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("1,2");
  }

  [Fact]
  public void Select_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [0,1,2]|select('truthy')|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("1,2");
  }

  [Fact]
  public void Reject_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [0,1,2]|reject('truthy')|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("0");
  }

  [Fact]
  public void Selectattr_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for i in items|selectattr('active') %}{{ i.name }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "a", ["active"] = true },
        new Dictionary<string, object?> { ["name"] = "b", ["active"] = false }
      }
    });
    result.Should().Be("a");
  }

  [Fact]
  public void Selectattr_WithTest_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for i in items|selectattr('val', 'gt', 5) %}{{ i.name }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "a", ["val"] = 10 },
        new Dictionary<string, object?> { ["name"] = "b", ["val"] = 3 }
      }
    });
    result.Should().Be("a");
  }

  [Fact]
  public void Selectattr_RequiresAttr_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ []|selectattr }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*selectattr requires*");
  }

  [Fact]
  public void Rejectattr_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for i in items|rejectattr('active') %}{{ i.name }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "a", ["active"] = true },
        new Dictionary<string, object?> { ["name"] = "b", ["active"] = false }
      }
    });
    result.Should().Be("b");
  }

  [Fact]
  public void Rejectattr_RequiresAttr_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ []|rejectattr }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*rejectattr requires*");
  }

  [Fact]
  public void Unique_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ [1,2,1,3,2]|unique|join(',') }}");
    var result = tmpl.Render();
    result.Should().Be("1,2,3");
  }

  [Fact]
  public void Unique_WithAttribute_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% for i in items|unique(attribute='type') %}{{ i.name }}{% endfor %}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["type"] = "a", ["name"] = "1" },
        new Dictionary<string, object?> { ["type"] = "a", ["name"] = "2" },
        new Dictionary<string, object?> { ["type"] = "b", ["name"] = "3" }
      }
    });
    result.Should().Be("13");
  }

  [Theory]
  [InlineData("{{ []|min }}", "")]
  [InlineData("{{ []|max }}", "")]
  [InlineData("{{ [3,1,2]|min }}", "1")]
  [InlineData("{{ [3,1,2]|max }}", "3")]
  public void MinMax_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void MinMax_WithAttribute_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{% set minItem = items|min(attribute='val') %}{% set maxItem = items|max(attribute='val') %}{{ minItem.name }}-{{ maxItem.name }}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["name"] = "a", ["val"] = 10 },
        new Dictionary<string, object?> { ["name"] = "b", ["val"] = 5 }
      }
    });
    result.Should().Be("b-a");
  }

  [Theory]
  [InlineData("{{ [1,2,3]|sum }}", "6")]
  [InlineData("{{ [1,2,3]|sum(10) }}", "16")]
  [InlineData("{{ [1.5,2.5]|sum }}", "4")]
  public void Sum_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Sum_WithAttribute_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ items|sum(attribute='val') }}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["items"] = new List<object> {
        new Dictionary<string, object?> { ["val"] = 1 },
        new Dictionary<string, object?> { ["val"] = 2 }
      }
    });
    result.Should().Be("3");
  }

  [Theory]
  [InlineData("{{ 3.567|round }}", "4")]
  [InlineData("{{ 3.567|round(2) }}", "3.57")]
  [InlineData("{{ 3.2|round(method='ceil') }}", "4")]
  [InlineData("{{ 3.8|round(method='floor') }}", "3")]
  public void Round_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Attr_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ obj|attr('name') }}");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["obj"] = new Dictionary<string, object?> { ["name"] = "test" }
    });
    result.Should().Be("test");
  }

  [Fact]
  public void Attr_RequiresArg_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ {}|attr }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*attr requires*");
  }

  [Theory]
  [InlineData("{{ '<p>Hello</p>'|striptags }}", "Hello")]
  [InlineData("{{ '<div><p>A</p><p>B</p></div>'|striptags }}", "AB")]
  public void Striptags_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'line1\\nline2'|indent }}", "line1\n    line2")]
  [InlineData("{{ 'line1\\nline2'|indent(2) }}", "line1\n  line2")]
  [InlineData("{{ 'line1\\nline2'|indent(first=true) }}", "    line1\n    line2")]
  [InlineData("{{ 'line1\\n\\nline3'|indent(blank=true) }}", "line1\n    \n    line3")]
  public void Indent_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'hello world'|wordcount }}", "2")]
  [InlineData("{{ '  hello   world  '|wordcount }}", "2")]
  public void Wordcount_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'hello world test'|truncate(11) }}", "hello...")]
  [InlineData("{{ 'hello world'|truncate(20) }}", "hello world")]
  [InlineData("{{ 'hello world'|truncate(8, killwords=true) }}", "hello...")]
  [InlineData("{{ 'hello world'|truncate(15, leeway=5) }}", "hello world")]
  [InlineData("{{ 'hello'|truncate(10, end='!') }}", "hello")]
  public void Truncate_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Wordwrap_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'hello world test'|wordwrap(10) }}");
    var result = tmpl.Render();
    result.Should().Contain("\n");
  }

  [Fact]
  public void Wordwrap_WithOptions_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'hello world'|wordwrap(10, wrapstring='<br>') }}");
    var result = tmpl.Render();
    // Note: output is escaped since <br> is not safe
    result.Should().Contain("&lt;br&gt;");
  }

  [Theory]
  [InlineData("{{ 'ab'|center(6) }}", "  ab  ")]
  [InlineData("{{ 'abc'|center(2) }}", "abc")]
  public void Center_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("{{ 'Hello %s'|format('World') }}", "Hello World")]
  [InlineData("{{ 'Hello %(name)s'|format(name='World') }}", "Hello World")]
  public void Format_ShouldWork(string template, string expected) {
    var env = new Environment();
    var tmpl = env.TemplateFromString(template);
    var result = tmpl.Render();
    result.Should().Be(expected);
  }

  [Fact]
  public void Pprint_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ {'a':1}|pprint }}");
    var result = tmpl.Render();
    // pprint produces pretty JSON which gets escaped since it contains quotes
    result.Should().Contain("a");
  }

  [Fact]
  public void Xmlattr_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("<div{{ attrs|xmlattr }}></div>");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["attrs"] = new Dictionary<string, object?> { ["class"] = "test", ["id"] = "main" }
    });
    result.Should().Contain("class=\"test\"");
    result.Should().Contain("id=\"main\"");
  }

  [Fact]
  public void Xmlattr_SkipsNone_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("<div{{ attrs|xmlattr }}></div>");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["attrs"] = new Dictionary<string, object?> { ["class"] = "test", ["id"] = null }
    });
    result.Should().Contain("class=\"test\"");
    result.Should().NotContain("id=");
  }

  [Fact]
  public void Xmlattr_NoAutospace_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("<div{{ attrs|xmlattr(false) }}></div>");
    var result = tmpl.Render(new Dictionary<string, object?> {
      ["attrs"] = new Dictionary<string, object?> { ["class"] = "test" }
    });
    result.Should().Be("<divclass=\"test\"></div>");
  }

  [Fact]
  public void Replace_RequiresArgs_ShouldThrow() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 'x'|replace('a') }}");
    Action act = () => tmpl.Render();
    act.Should().Throw<TemplateError>().WithMessage("*replace requires*");
  }

  [Fact]
  public void String_Filter_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ 42|string }}");
    var result = tmpl.Render();
    result.Should().Be("42");
  }

  [Fact]
  public void Safe_Filter_ShouldWork() {
    var env = new Environment();
    var tmpl = env.TemplateFromString("{{ '<b>bold</b>'|safe }}");
    var result = tmpl.Render();
    result.Should().Be("<b>bold</b>");
  }
}
