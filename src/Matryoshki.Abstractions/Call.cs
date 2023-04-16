namespace Matryoshki.Abstractions;

public class Call<TResult>
{
    public string MemberName { get; }

    public bool IsProperty { get; }
    public bool IsGetter { get; }
    public bool IsSetter { get; }
    public bool IsMethod { get; }

    private Call()
    {
        MemberName = default!;
    }

    public TResult Forward()
    {
        return default!;
    }

    public dynamic DynamicForward()
    {
        return default!;
    }

    public Task<TResult> ForwardAsync()
    {
        return Task.FromResult<TResult>(default!);
    }

    public dynamic Pass(object? any)
    {
        return default!;
    }

    public T Pass<T>(object? any)
    {
        return default!;
    }

    public string[] GetParameterNames()
    {
        return default!;
    }

    public Argument<T>? GetFirstArgumentOfType<T>()
    {
        return default!;
    }

    public Argument<T>[] GetArgumentsOfType<T>()
    {
        return default!;
    }

    public T? GetFirstArgumentValueOfType<T>()
    {
        return default!;
    }

    public T[] GetArgumentsValuesOfType<T>()
    {
        return default!;
    }

    public TResult GetSetterValue()
    {
        return default!;
    }
}