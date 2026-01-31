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
  case "case2":
    Console.WriteLine(new Case2().Render());
    break;
  case "case3":
    Console.WriteLine(new Case3().Render());
    break;
  case "case4":
    Console.WriteLine(new Case4().Render());
    break;
  case "case5":
    Console.WriteLine(new Case5().Render());
    break;
  case "case6":
    Console.WriteLine(new Case6().Render());
    break;
  case "case7":
    Console.WriteLine(new Case7().Render());
    break;
  case "case8":
    Console.WriteLine(new Case8().Render());
    break;
  case "case9":
    Console.WriteLine(new Case9().Render());
    break;
  case "case10":
    Console.WriteLine(new Case10().Render());
    break;
  case "case11":
    Console.WriteLine(new Case11().Render());
    break;
  default:
    Console.WriteLine("invalid input");
    break;
}
