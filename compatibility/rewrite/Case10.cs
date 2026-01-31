namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Macro
public class Case10
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case10/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case10Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case10Context
{
  public string Name { get; set; } = "";
}
