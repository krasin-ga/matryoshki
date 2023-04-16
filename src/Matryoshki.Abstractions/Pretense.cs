namespace Matryoshki.Abstractions;

public static class Pretense
{
    public static T Pretend<T>(this object? obj)
        => throw new NotSupportedException();
}