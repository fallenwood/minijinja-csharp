namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Range function
public class Case5
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case5/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case5Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case5Context
{
  public string Name { get; set; } = "";
}
