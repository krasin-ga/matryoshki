using Matryoshki.Abstractions;

namespace Matryoshki.Tests.ExternalAdornments;

/// <summary>
/// That adornment will be "compiled" into an attribute
/// </summary>
public class ExternalAdornment : IAdornment
{
    public bool Executed { get; set; }
    public bool ExecutedAsync { get; set; }

    public async Task<TResult> AsyncMethodTemplate<TResult>(Call<TResult> call)
    {
        ExecutedAsync = true;
        return await call.ForwardAsync();
    }
    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        Executed = true;
        return call.Forward();
    }
}