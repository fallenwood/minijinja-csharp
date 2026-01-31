namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

public class Case0
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case0/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case0Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case0Context
{
  public string Name { get; set; } = "";
}
