using System.Threading.Tasks;
using Matryoshki.Abstractions;
using Matryoshki.Tests.ExternalAdornments;
using Xunit;

namespace Matryoshki.Tests.CompiledAdornments;

public class CompiledAdornmentsTest
{
    [Fact]
    public void SyncMethod_MustBeAbleToDecorateWithAdornmentsDefinedInExternalPackages()
    {
        Matryoshka<ITestInterface>
            .With<ExternalAdornment>()
            .Name<ExternalAdornmentDecorator>();

        var decorator = new ExternalAdornmentDecorator(
            new TestImplementation());

        decorator.DoNothing();

        Assert.True(decorator.Executed_Δ);
    }

    [Fact]
    public async Task TaskMethod_MustBeAbleToDecorateWithAdornmentsDefinedInExternalPackages()
    {
        var decorator = new ExternalAdornmentDecorator(
            new TestImplementation());

        await decorator.DoNothingAsync();

        Assert.True(decorator.ExecutedAsync_Δ);
    }

    [Fact]
    public async Task TaskWithResultMethod_MustBeAbleToDecorateWithAdornmentsDefinedInExternalPackages()
    {
        var decorator = new ExternalAdornmentDecorator(
            new TestImplementation());

        await decorator.Return1Async();
        Assert.True(decorator.ExecutedAsync_Δ);
    }

    public interface ITestInterface
    {
        public void DoNothing()
        {
        }

        public Task DoNothingAsync()
        {
            return Task.CompletedTask;
        }

        public Task<int> Return1Async()
        {
            return Task.FromResult(1);
        }
    }

    private record TestImplementation : ITestInterface;
}