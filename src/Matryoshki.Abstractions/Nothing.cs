namespace Matryoshki.Abstractions;

public class Nothing
{
    public static Nothing Instance { get; } = new();

    private Nothing()
    {
    }

    public static Nothing FromPropertyAction<TInstance, TValue>(
        in TInstance @this,
        in TValue value,
        Action<TInstance, TValue> action)
    {
        action(@this, value);

        return Instance;
    }

    public static Nothing FromIndexerAction<TInstance, TKey, TValue>(
        in TInstance @this,
        in TKey key,
        in TValue value,
        Action<TInstance, TKey, TValue> action)
    {
        action(@this, key, value);

        return Instance;
    }
}