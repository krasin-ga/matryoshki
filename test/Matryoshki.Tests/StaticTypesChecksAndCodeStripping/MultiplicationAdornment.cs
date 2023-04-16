using Matryoshki.Abstractions;

namespace Matryoshki.Tests.StaticTypesChecksAndCodeStripping;

public class MultiplicationAdornment : IAdornment
{
    private double Multiplier { get; }

    public MultiplicationAdornment(double multiplier)
    {
        Multiplier = multiplier;
    }

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
            return call.Pass((TResult)(result * Multiplier));

        return result;
    }
}