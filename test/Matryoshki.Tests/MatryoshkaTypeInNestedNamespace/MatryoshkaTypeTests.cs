using System;
using System.Linq.Expressions;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.MatryoshkaTypeInNestedNamespace;

public class MatryoshkaTypeTests
{
    [Fact]
    public void MustCorrectlyResolveTypesInCompiledExpressions()
    {
        static Abstractions.MatryoshkaType Decorate(Expression<Func<Abstractions.MatryoshkaType>> expression)
            => expression.Compile()();

        var matryoshkaType = Decorate(
           () => Decorate<ICloneable>.With<SimpleAdornment>());

        Assert.Equal(
            expected: typeof(ICloneableWithSimpleAdornment),
            actual: matryoshkaType.Type);

        Assert.Equal(
            expected: typeof(ICloneable),
            actual: matryoshkaType.Target);
    }
}