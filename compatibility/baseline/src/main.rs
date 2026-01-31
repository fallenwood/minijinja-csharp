use minijinja::{Environment, context};

fn main() {
}

fn case1() {
    let mut env = Environment::new();
    env.add_template("hello.txt", "Hello {{ name }}!").unwrap();
    let template = env.get_template("hello.txt").unwrap();
    println!("{}", template.render(context! { name => "World" }).unwrap());
}
