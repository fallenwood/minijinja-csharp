using Rewrite;

if (args.Length < 1) {
  Console.WriteLine("invalid input");
  return;
}

switch (args[0]) {
  case "case0":
    Console.WriteLine(new Case0().Render());
    break;
  case "case1":
    Console.WriteLine(new Case1().Render());
    break;
  default:
    Console.WriteLine("invalid input");
    break;
}
