using Matryoshki.Abstractions;

namespace Matryoshki.Tests.Arguments;

public class ArgumentsTestingAdornment<T> : IAdornment
{
    public string[]? ParameterNames { get; set; }
    public T[] ValuesOfT { get; private set; } = null!;
    public T? FirstValueOfT { get; set; }
    public Argument<T>? FirstArgumentOfT { get; set; }
    public Argument<T>[]? ArgumentsOfT { get; private set; }
    public object? SetterValue { get; set; }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        ArgumentsOfT = call.GetArgumentsOfType<T>();
        FirstArgumentOfT = call.GetFirstArgumentOfType<T>();
        FirstValueOfT = call.GetFirstArgumentValueOfType<T>();
        ValuesOfT = call.GetArgumentsValuesOfType<T>();
        ParameterNames = call.GetParameterNames();
        SetterValue = call.GetSetterValue();

        return call.Forward();
    }

}