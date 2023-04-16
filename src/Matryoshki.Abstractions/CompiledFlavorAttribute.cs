namespace Matryoshki.Abstractions;


[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class CompiledAdornmentAttribute : Attribute
{
    public string FullAdornmentName { get; }
    public string ClassName { get; }
    public string Body { get; }

    public CompiledAdornmentAttribute(
        string fullAdornmentName,
        string className,
        string body)
    {
        FullAdornmentName = fullAdornmentName;
        Body = body;
        ClassName = className;
    }
}