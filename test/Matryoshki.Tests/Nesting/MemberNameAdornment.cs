using Matryoshki.Abstractions;

namespace Matryoshki.Tests.Nesting;

public class MemberNameAdornment : IAdornment
{
    public string? MemberName { get; private set; }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        MemberName = call.MemberName;
        return call.Forward();
    }
}