namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Default filter
public class Case9
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case9/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case9Context { Value = "Hello" });
  }
}

[MiniJinjaContext]
partial class Case9Context
{
  public string? Value { get; set; }
}
