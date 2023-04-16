namespace Matryoshki.Abstractions;

public readonly struct Argument<T>
{
    public string Name { get; }
    public T Value { get; }

    public Argument(string name, T value)
    {
        Name = name;
        Value = value;
    }

    public void Deconstruct(out string name, out T value)
    {
        name = Name;
        value = Value;
    }
}