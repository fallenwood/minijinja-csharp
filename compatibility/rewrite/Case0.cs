namespace Rewrite;

using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

public class Case0
{
  public string Render()
  {
    var env = new MJEnvironment();
  }
}

[MiniJinjaContext]
partial class Case0Context
{
  public string Name { get; set; } = "";
}
