using System.Diagnostics.CodeAnalysis;
using Matryoshki.Abstractions;

namespace Matryoshki.Tests;

[ExcludeFromCodeCoverage]
public class SimpleAdornment : IAdornment
{
    public bool WasExecuted { get; set; }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        WasExecuted = true;
        return call.Forward();
    }
}