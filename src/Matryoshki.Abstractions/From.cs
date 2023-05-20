namespace Matryoshki.Abstractions;

public static class From<T>
{
    public static Type ExtractInterface<TName>()
    {
        return typeof(TName);
    }
}