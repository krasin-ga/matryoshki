using Matryoshki.Abstractions;
using MatryoshkiGenerated.TestNesting;
using Xunit;

namespace Matryoshki.Tests.Nesting;

public class NestingTest
{
    [Fact]
    public void MustCorrectlyCreateNestedDecorations()
    {
        var types = Matryoshka<ITestInterface>.WithNesting<TestNesting>();

        var innerDecorator = new ITestInterfaceWithMemberNameAdornment(
            new TestImplementation());

        ITestInterface decoratorAsBaseInterface = innerDecorator;

        var outerDecorator = new ITestInterfaceWithSimpleAdornment(
            decoratorAsBaseInterface
        );

        outerDecorator.DoNothing(10, out _);

        Assert.Equal(
            expected: nameof(ITestInterface.DoNothing),
            actual: innerDecorator.MemberName_Δ);
        Assert.True(outerDecorator.WasExecuted_Δ);

        Assert.Equal(
            expected: typeof(ITestInterfaceWithSimpleAdornment),
            actual: types.Decorators[0]);

        Assert.Equal(
            expected: typeof(ITestInterfaceWithMemberNameAdornment),
            actual: types.Decorators[1]);

        Assert.Equal(
            expected: typeof(ITestInterface),
            actual: types.Target);
    }

    [Fact]
    public void MustCorrectlyCreateStrictNestedDecorations()
    {
        var types = Matryoshka<ITestInterface2>.WithStrictNesting<TestNesting>();

        var innerDecorator = new ITestInterface2WithMemberNameAdornment(
            new TestImplementation2());

        var outerDecorator = new ITestInterface2WithSimpleAdornment(
            innerDecorator
        );

        outerDecorator.DoNothing(10, out _);

        Assert.Equal(
            expected: nameof(ITestInterface2.DoNothing),
            actual: innerDecorator.MemberName_Δ);
        Assert.True(outerDecorator.WasExecuted_Δ);

        Assert.Equal(
            expected: typeof(ITestInterface2WithSimpleAdornment),
            actual: types.Decorators[0]);

        Assert.Equal(
            expected: typeof(ITestInterface2WithMemberNameAdornment),
            actual: types.Decorators[1]);

        Assert.Equal(
            expected: typeof(ITestInterface2),
            actual: types.Target);
    }

    public interface ITestInterface
    {
        public void DoNothing(in int inParameter, out double outParameter)
        {
            outParameter = default;
        }
    }

    public interface ITestInterface2
    {
        public void DoNothing(in int inParameter, out double outParameter)
        {
            outParameter = default;
        }
    }

    private record TestImplementation : ITestInterface;
    private record TestImplementation2 : ITestInterface2;
}