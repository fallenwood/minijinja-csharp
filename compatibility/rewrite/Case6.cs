namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Set statement
public class Case6
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case6/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case6Context { Name = "Ririko" });
  }
}

[MiniJinjaContext]
partial class Case6Context
{
  public string Name { get; set; } = "";
}
