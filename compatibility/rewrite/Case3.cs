namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// If/else
public class Case3
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case3/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case3Context { Show = true, Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case3Context
{
  public bool Show { get; set; }
  public string Name { get; set; } = "";
}
