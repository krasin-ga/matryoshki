using Matryoshki.Abstractions;
using MatryoshkiGenerated.TestNesting;
using Xunit;

namespace Matryoshki.Tests.Nesting;

public class CompiledAdornmentsTest
{
    [Fact]
    public void MustBeAbleToDecorateWithAdornmentsDefinedInExternalPackages()
    {
        var types = Matryoshka<ITestInterface>
            .WithNesting<TestNesting>();

        var innerDecorator = new ITestInterfaceWithMemberNameAdornment(
            new TestImplementation());

        var outerDecorator = new ITestInterfaceWithSimpleAdornment(
            innerDecorator
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

    public interface ITestInterface
    {
        public void DoNothing(in int inParameter, out double outParameter)
        {
            outParameter = default;
        }
    }

    private record TestImplementation : ITestInterface;
}