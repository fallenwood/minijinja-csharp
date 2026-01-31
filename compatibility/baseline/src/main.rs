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
    "case2" => case2(),
    "case3" => case3(),
    "case4" => case4(),
    "case5" => case5(),
    "case6" => case6(),
    "case7" => case7(),
    "case8" => case8(),
    "case9" => case9(),
    "case10" => case10(),
    "case11" => case11(),
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

// For loop
fn case2() {
  let template_content = fs::read_to_string("../cases/case2/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { items => vec!["apple", "banana", "cherry"] }).unwrap());
}

// If/else
fn case3() {
  let template_content = fs::read_to_string("../cases/case3/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { show => true, name => "Ririko" }).unwrap());
}

// Filters
fn case4() {
  let template_content = fs::read_to_string("../cases/case4/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}

// Range function
fn case5() {
  let template_content = fs::read_to_string("../cases/case5/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}

// Set statement
fn case6() {
  let template_content = fs::read_to_string("../cases/case6/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}

// Object access
fn case7() {
  let template_content = fs::read_to_string("../cases/case7/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { user => context! { name => "Ririko", age => 25 } }).unwrap());
}

// Loop variables
fn case8() {
  let template_content = fs::read_to_string("../cases/case8/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { items => vec!["apple", "banana", "cherry"] }).unwrap());
}

// Default filter
fn case9() {
  let template_content = fs::read_to_string("../cases/case9/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { value => "Hello" }).unwrap());
}

// Macro
fn case10() {
  let template_content = fs::read_to_string("../cases/case10/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { name => "Ririko" }).unwrap());
}

// Nested loops
fn case11() {
  let template_content = fs::read_to_string("../cases/case11/template.txt").unwrap();
  let mut env = Environment::new();
  env.add_template("template", &template_content).unwrap();
  let template = env.get_template("template").unwrap();
  println!("{}", template.render(context! { matrix => vec![vec![1, 2, 3], vec![4, 5, 6], vec![7, 8, 9]] }).unwrap());
}
