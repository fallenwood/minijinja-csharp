namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Loop variables
public class Case8
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case8/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case8Context { Items = ["apple", "banana", "cherry"] });
  }
}

[MiniJinjaContext]
partial class Case8Context
{
  public List<string> Items { get; set; } = [];
}
