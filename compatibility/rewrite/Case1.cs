namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

public class Case1
{
  public string Render()
  {
    var baseContent = File.ReadAllText("../cases/case1/base.txt");
    var templateContent = File.ReadAllText("../cases/case1/template.txt");
    var env = new MJEnvironment();
    env.AddTemplate("base.txt", baseContent);
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case1Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case1Context
{
  public string Name { get; set; } = "";
}
