using System;
using System.Linq.Expressions;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests;

public class MatryoshkaTypeTests
{
    [Fact]
    public void MustCorrectlyResolveTypesInCompiledExpressions()
    {
        static MatryoshkaType Decorate(Expression<Func<Abstractions.MatryoshkaType>> expression)
            => expression.Compile()();

        var matryoshkaType = Decorate(
            () => Decorate<IDisposable>.With<SimpleAdornment>());

        Assert.Equal(
            expected: typeof(IDisposableWithSimpleAdornment),
            actual: matryoshkaType.Type);

        Assert.Equal(
            expected: typeof(IDisposable),
            actual: matryoshkaType.Target);
    }
}