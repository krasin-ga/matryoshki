using Matryoshki.Abstractions;
using Matryoshki.Tests.ExternalAdornments;
using Xunit;

namespace Matryoshki.Tests.CompiledAdornments;

public class CompiledAdornmentsTest
{
    [Fact]
    public void MustBeAbleToDecorateWithAdornmentsDefinedInExternalPackages()
    {
        Matryoshka<ITestInterface>
            .With<ExternalAdornment>()
            .Name<ExternalAdornmentDecorator>();


        var decorator = new ExternalAdornmentDecorator(
            new TestImplementation());

        decorator.DoNothing();

        Assert.True(decorator.Executed_Δ);
    }

    public interface ITestInterface
    {
        public void DoNothing()
        {
        }
    }

    private record TestImplementation : ITestInterface;
}