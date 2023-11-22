namespace Matryoshki.Abstractions;

public readonly struct MatryoshkaType
{
    private readonly Func<Type> _locateType;
    public Type Type => _locateType();
    public Type Target { get; }

    public MatryoshkaType(Type target, Func<Type> locateType)
    {
        Target = target;
        _locateType = locateType;
    }

    public static implicit operator Type(MatryoshkaType matryoshkaType)
        => matryoshkaType.Type;

    public MatryoshkaType Name<T>()
    {
        return new MatryoshkaType(Target, () => typeof(T));
    }
}