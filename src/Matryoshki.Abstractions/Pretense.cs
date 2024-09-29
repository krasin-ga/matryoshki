namespace Matryoshki.Abstractions;

public static class Pretense
{
    /// <summary>
    /// This method is used to bypass type checking in the method template and will be stripped from generated decorator 
    /// </summary>
    public static T Pretend<T>(this object? obj)
        => throw new NotSupportedException();
}