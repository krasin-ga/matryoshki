# Tips

## Always use var

This is necessary because the code used when writing the template will be used to generate methods for arbitrary return types.

## Members of an adornment

An adornment can have any members that will be transferred to the decorator. The constructors will be expanded with a target type parameter. To prevent conflicts, all member names will be prefixed with `_Δ`.

## Obtaining call metadata
Call metadata can be obtained using the properties and methods of the Call parameter: `MemberName`, `IsProperty`, `IsGetter`, `IsSetter`, `IsMethod`, `GetParameterNames()`, `GetFirstArgumentOfType<T>()`, `GetArgumentsOfType<T>()`, `GetFirstArgumentValueOfType<T>()`, `GetArgumentsValuesOfType<T>()`, `GetSetterValue()`.

## The Nothing type

The type Nothing is used as the argument for TResult in cases where the original method returns void or is asynchronous and returns Task/ValueTask.

## Using type checks

Method templates support static type checks using `if` statements and `typeof()` operators.

```C#
public TResult MethodTemplate<TResult>(Call<TResult> call)
{
    var result = call.Forward();
    //If TResult is Nothing then rest of methods body will be stripped away
    if(typeof(TResult) == typeof(Nothing))
        return result;

    //If TResult is string then result will be written to console
    if(typeof(TResult) == typeof(string))
        Console.WriteLine(result);

    return result;
}
```

## Overriding the return value
To do this, simply return a compatible value. To bypass compiler checks if necessary, you can use `call.DynamicForward` and `call.Pass`. The expression inside `Pass()` will be inserted into the resulting method as is. You can also use `Pretend<T>` exension method.

```C#
public TResult MethodTemplate<TResult>(Call<TResult> call)
{
    //Dynamic is used to bypass compiler checks
    //actual variable in decorator will be of type TResult
    var result = call.DynamicForward();

    if (typeof(TResult) == typeof(int)
        || typeof(TResult) == typeof(double)
        || typeof(TResult) == typeof(float)
        || typeof(TResult) == typeof(uint)
        || typeof(TResult) == typeof(ulong)
        || typeof(TResult) == typeof(ushort)
        || typeof(TResult) == typeof(byte)
        || typeof(TResult) == typeof(long))
        return call.Pass((TResult)(result * 10));

    return result;
}
```

Examples of expanding templates for cases where methods return `int` and `string`:

```C#
public int SampleIntMethod()
{
    var result = _inner.SampleIntMethod();
    return (TResult)(result * 10);
}

public string SampleStringMethod()
{
    var result = _inner.SampleStringMethod();
    return result;
}
```

## Asynchronous Templates
You can define a template for asynchronous methods. This template will be applied to methods that return Task or ValueTask.
```C#
public class CachingAdornment : IAdornment
{
    private readonly ICache _cache;

    public CachingAdornment(ICache cache)
    {
        _cache = cache;
    }

    public async ValueTask<TResult> AsyncMethodTemplate<TResult>(Call<TResult> call)
    {
        if (typeof(TResult) == typeof(Nothing))
            return await call.NextAsync();

        var cacheKey = _cache.CalculateKey(call.GetArgumentsOfType<object>());
        var cachedValue = await _cache.GetAsync(cacheKey);
        if (cachedValue.HasValue)
            return cachedValue.Value;

        var value = await call.NextAsync();
        _ = _cache.SetAsync(cacheKey, value);

        return value;
    }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        if (typeof(TResult) == typeof(Nothing))
            return call.Next();

        var cacheKey = _cache.CalculateKey(call.GetArgumentsOfType<object>());
        var cachedValue = _cache.Get(cacheKey);
        if(cachedValue.HasValue)
            return cachedValue.Value;

        var value = call.Next();
        _cache.Set(cacheKey, value);

        return value;
    }
}
```

## Generic Adornments
Decorators can be generic, but you need to specify type arguments when decorating:
```C#
Matryoshka<ITestInterface>
    .With<ArgumentsTestingAdornment<string>>()
    .Name<StringArgumentsTestingDecorator>();
```

## Compiled Adornments
If the adornment is defined in a project (or package) different from the one where it is being used, its syntax tree must be serialized for proper consumption. To do this, both the `Matryoshki.Abstractions` and `Matryoshki.Generators` packages must be referenced.

<p align="center">
<img width="128" src="assets/footer.png"/>
</p>
