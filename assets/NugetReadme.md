# ![Logo](https://raw.githubusercontent.com/krasin-ga/matryoshki/main/assets/logo-128.png) Matryoshki

### Metaprogramming framework based on C# source generators

"Matryoshki" (Матрёшки, Matryoshkas) is a set of abstractions and C# source generators that allow you to describe type-agnostic templates and create decorators based on them. All of this works at the coding stage, which significantly improves productivity, simplifies development and debugging (the source code of the generated classes can be immediately viewed), and allows the library to be used in limited AOT runtimes (such as AOT iOS Unity runtime).

## Getting Started

### Installation

The first step is to add two packages to the target project:

``` bash
dotnet add package Matryoshki
dotnet add package Matryoshki.Abstractions
```

Once the packages are installed, you can proceed with creating adornments.


### Adornments

<img src="assets/flower.png" style="float: right;  max-height: 64px; margin: 10px; margin-top: 5px" onerror="this.style.display='none'"/>

Adornments act as blueprints for creating type-agnostic decorators. They consist of a method template and can contain arbitrary members. Rather than being instantiated as objects, the code of adornment classes is directly injected into the decorator classes.

To create an adornment you need to create a class that implements `IAdornment`. As a simple example, you can create an adornment that outputs the name of the decorated member to the console:

``` C#
public class HelloAdornment : IAdornment
{
    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        Console.WriteLine($"Hello, {call.MemberName}!");
        return call.Forward();
    }
}
```

When creating a decorated method, `call.Forward()` will be replaced with a call to the implementation. And `TResult` will have the type of the actual return value. For `void` methods, a special type `Nothing` will be used.

#### Asynchronous method templates

Asynchronous templates can be defined by implementing the `AsyncMethodTemplate` method, which will be used to decorate methods that return `Task` or `ValueTask`. 

Note that asynchronous templates are optional, and async methods will still be decorated because an `AsyncMethodTemplate` will be automatically created from the `MethodTemplate` by awaiting the `Forward*` method invocations.

More tips for writing adornments can be found here: [tips](https://github.com/krasin-ga/Tips.md).


### Decoration

Once we have an adornment, we can create our first matryoshkas.

Suppose we have two interfaces that we would like to apply our `HelloAdornment` to:

``` C#
interface IFoo
{
    object Foo(object foo) => foo;
}
record Foo : IFoo;

interface IBar
{
    Task BarAsync() => Task.Delay(0);
}
record Bar : IFoo;
```

To create matryoshkas, you just need to write their specification in any appropriate location:

``` C#
Matryoshka<IFoo>
    .With<HelloAdornment>()
    .Name<FooMatryoshka>();

Decorate<IBar> // you can use Decorate<> alias if you prefer
    .With<HelloAdornment>()
    .Name<BarMatryoshka>();
```

Done! Now we can test the generated classes:

``` C#
var fooMatryoshka = new FooMatryoshka(new Foo());
var barMatryoshka = new BarMatryoshka(new Bar());

fooMatryoshka.Foo(); // "Hello, Foo!" will be written to console
barMatryoshka.Bar(); // "Hello, Bar!" will be written to console
```

In a production environment, you will likely prefer to use DI containers that support decoration (Grace, Autofac, etc.) or libraries like [Scrutor](https://github.com/khellang/Scrutor). Here's an example of using matryoshkas together with Scrutor:

``` C#
using Scrutor;
using Matryoshki.Abstractions;

public static class MatryoshkaScrutorExtensions
{
    public static IServiceCollection DecorateWithMatryoshka(
        this IServiceCollection serviceCollection,
        Expression<Func<MatryoshkaType>> expression)
    {
        var matryoshkaType = expression.Compile()();

        services.Decorate(matryoshkaType.Target, matryoshkaType.Type);
    }

    public static IServiceCollection DecorateWithNestedMatryoshkas(
        this IServiceCollection serviceCollection,
        Expression<Func<MatryoshkaTypes>> expression)
    {
        var matryoshkaTypes = expression.Compile()();

        foreach (var type in matryoshkaTypes)
            services.Decorate(matryoshkaTypes.Target, type);
    }
}

internal static class Example
{
    internal static IServiceCollection DecorateBar(
        this IServiceCollection services)
    {
        return services.DecorateWithMatryoshka(
            () => Matryoshka<IBar>.With<HelloAdornment>());
    }
}
```

### Chains of decorations with INesting<T1, ..., TN>

Reusable decoration chains can be described by creating a type that implements `INesting<T1, ..., TN>`:

``` C#
public record ObservabilityNesting : INesting<MetricsAdornment, LoggingAdornment, TracingAdornment>;
```

You can generate the classes using it as follows:

``` C#
static IServiceCollection DecorateFoo(IServiceCollection services)
{
    //assuming that you are using MatryoshkaScrutorExtensions
    return services.DecorateWithNestedMatryoshkas(
        () => Matryoshka<IBar>.WithNesting<ObservabilityNesting>());
}
```

It is not possible to assign names to the classes when using `INesting`. The generated types will be located in the `MatryoshkiGenerated.{NestingName}` namespace and have names in the format **TargetTypeName***With***AdornmentName**.

## Limitations

* Do not use a variable named `value`, as this can conflict with a property setter.
* The `call` parameter should not be passed to other methods.
* `default` cannot be used without specifying a type argument.
* To apply decorations, the members must be abstract or virtual.
* The decoration expression must be computable at compile time and written with a single statement
* Pattern matching will not always work

## License

This project is licensed under the [MIT license](https://github.com/krasin-ga/LICENSE).



## Quick links


* [Repository](https://github.com/krasin-ga/matryoshki)