# MiniJinja-CSharp

[![CI](https://github.com/fallenwood/minijinja-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/fallenwood/minijinja-csharp/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/fallenwood/minijinja-csharp/graph/badge.svg)](https://codecov.io/gh/fallenwood/minijinja-csharp)
[![NuGet](https://img.shields.io/nuget/v/minijinja-csharp.svg)](https://www.nuget.org/packages/minijinja-csharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/minijinja-csharp.svg)](https://www.nuget.org/packages/minijinja-csharp)

A C# port of [MiniJinja](https://github.com/mitsuhiko/minijinja) template engine. This library is based on [minijinja-go](https://github.com/mitsuhiko/minijinja/tree/main/minijinja-go).

## Installation

```bash
dotnet add package minijinja-csharp
```

## Quick Start

```csharp
using MiniJinja;
using MJEnvironment = MiniJinja.Environment;

using var env = new MJEnvironment();
env.AddTemplate("hello", "Hello {{ name }}!");
var tmpl = env.GetTemplate("hello");
var result = tmpl.Render(new Dictionary<string, object?> { ["name"] = "World" });
Console.WriteLine(result); // Output: Hello World!
```

## License

Apache-2.0
