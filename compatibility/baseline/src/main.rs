use minijinja::{Environment, context};
use std::env;
use std::fs;

fn main() {
  let args: Vec<String> = env::args().collect();
  if args.len() < 2 {
    println!("invalid input");
    return;
  }

  match args[1].as_str() {
    "case0" => case0(),
    "case1" => case1(),
    _ => println!("invalid input"),
  }
}

fn case0() {
  let template_content = fs::read_to_string("../cases/case0/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}

fn case1() {
  let base_content = fs::read_to_string("../cases/case1/base.txt").unwrap();
  let template_content = fs::read_to_string("../cases/case1/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("base.txt", &base_content).unwrap();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}
