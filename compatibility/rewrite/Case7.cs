namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

// Object access
public class Case7
{
  public string Render()
  {
    var templateContent = File.ReadAllText("../cases/case7/template.txt");
    var env = new MJEnvironment();
    var template = env.TemplateFromString(templateContent);
    return template.Render(new Case7Context { User = new UserInfo { Name = "Ririko", Age = 25 } });
  }
}

[MiniJinjaContext]
partial class Case7Context
{
  public UserInfo User { get; set; } = new();
}

[MiniJinjaContext]
public partial class UserInfo
{
  public string Name { get; set; } = "";
  public int Age { get; set; }
}
