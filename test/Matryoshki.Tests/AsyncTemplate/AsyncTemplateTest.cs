using System;
using System.Threading.Tasks;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.AsyncTemplate;

public class AsyncTemplateTest
{
    [Fact]
    public async Task MustUseAsyncTemplateForMethodsThatReturnTasks()
    {
        Matryoshka<ITestInterface>
            .With<ChangeDoubleTaskResultWithDelayAsyncAdornment>()
            .Name<AsyncTemplateTestDecorator>();

        const double expected = 100d;
        var decorator = new AsyncTemplateTestDecorator(
            result: expected,
            new TestImplementation());

        await decorator.DoNothingTask();
        Assert.True(decorator.ExecutedAsync_Δ);

        await decorator.DoNothingValueTask();
        Assert.True(decorator.ExecutedAsync_Δ);

        var syncResult = decorator.GetDoubleSync();
        Assert.False(decorator.ExecutedAsync_Δ);

        var asyncResult = await decorator.GetDoubleAsync();
        Assert.True(decorator.ExecutedAsync_Δ);

        var asyncValueTaskResult = await decorator.GetDoubleAsyncValueTask();
        Assert.True(decorator.ExecutedAsync_Δ);

        Assert.Equal(
            expected: expected,
            actual: asyncResult);

        Assert.Equal(
            expected: expected,
            actual: asyncValueTaskResult);

        Assert.Equal(
            expected: default,
            actual: syncResult);
    }

    public interface ITestInterface
    {
        public Task DoNothingTask()
        {
            return Task.CompletedTask;
        }

        public ValueTask DoNothingValueTask()
        {
            return ValueTask.CompletedTask;
        }

        public Task<double> GetDoubleAsync()
        {
            return Task.FromResult(0d);
        }

        public ValueTask<SomeUserDefinedType> GetSomeCustomType()
        {
            return ValueTask.FromResult<SomeUserDefinedType>(null!);
        }

        public ValueTask<double> GetDoubleAsyncValueTask()
        {
            return default;
        }

        public double GetDoubleSync()
        {
            return default;
        }
    }

    public class SomeUserDefinedType
    {
    }

    private record TestImplementation : ITestInterface;
}