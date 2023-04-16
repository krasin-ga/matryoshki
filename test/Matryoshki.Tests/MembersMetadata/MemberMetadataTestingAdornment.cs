using Matryoshki.Abstractions;

namespace Matryoshki.Tests.Members;

public class MemberMetadataTestingAdornment : IAdornment
{
    public string? MemberName { get; set; }
    public bool IsMethod { get; set; }
    public bool IsSetter { get; set; }
    public bool IsGetter { get; set; }
    public bool IsProperty { get; set; }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        IsProperty = call.IsProperty;
        IsGetter = call.IsGetter;
        IsSetter = call.IsSetter;
        IsMethod = call.IsMethod;
        MemberName = call.MemberName;
        return call.Forward();
    }
}