using System.Collections;

namespace Matryoshki.Abstractions;

public class MatryoshkaTypes: IEnumerable<Type>
{
    public Type Target { get; }
    public Type[] Decorators { get; }


    public MatryoshkaTypes(Type target, Type[] decorators)
    {
        Target = target;
        Decorators = decorators;
    }

    public IEnumerator<Type> GetEnumerator()
    {
        return Decorators.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}