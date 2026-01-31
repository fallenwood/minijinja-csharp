namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Filters
public class Case4
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case4/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case4Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case4Context
{
  public string Name { get; set; } = "";
}
