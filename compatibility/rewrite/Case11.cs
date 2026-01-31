namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Nested loops
public class Case11
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case11/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case11Context {
      Matrix = [
        [1, 2, 3],
        [4, 5, 6],
        [7, 8, 9]
      ]
    });
  }
}

[MiniJinjaContext]
partial class Case11Context
{
  public List<List<int>> Matrix { get; set; } = [];
}
