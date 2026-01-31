namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// For loop
public class Case2
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case2/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case2Context { Items = ["apple", "banana", "cherry"] });
  }
}

[MiniJinjaContext]
partial class Case2Context
{
  public List<string> Items { get; set; } = [];
}
