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


        var syncResult = decorator.GetDoubleSync();
        var asyncResult = await decorator.GetDoubleAsync();
        var asyncValueTaskResult = await decorator.GetDoubleAsyncValueTask();

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
        public ValueTask<double> GetDoubleAsync()
        {
            return default;
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

    private record TestImplementation : ITestInterface;
}