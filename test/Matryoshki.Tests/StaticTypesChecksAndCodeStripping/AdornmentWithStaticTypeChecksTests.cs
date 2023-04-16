using System;
using System.Threading.Tasks;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.StaticTypesChecksAndCodeStripping;

public class AdornmentWithStaticTypeChecksTests
{
    static AdornmentWithStaticTypeChecksTests()
    {
        Matryoshka<ITestInterface>
            .With<MultiplicationAdornment>()
            .Name<TestMultiplicationDecorator>();
    }

    [Fact]
    public void MustCorrectlyDecorateSyncMethodsAndPropertiesForAdornmentThatHasStaticTypeChecks()
    {
        ITestInterface decorated
            = new TestImplementation
              {
                  DoubleProperty = 20,
                  IntProperty = 100,
                  ObjectProperty = "Test"
              };

        const int multiplier = 10;

        ITestInterface decorator = new TestMultiplicationDecorator(
            multiplier: multiplier,
            decorated
        );

        Assert.Equal(expected: decorated.DoubleProperty * multiplier,
                     actual: decorator.DoubleProperty);

        Assert.Equal(expected: decorated.GetInt() * multiplier,
                     actual: decorator.GetInt());

        Assert.Equal(expected: decorated.IntProperty * multiplier,
                     actual: decorator.IntProperty);

        Assert.Equal(expected: decorated.ObjectProperty,
                     actual: decorator.ObjectProperty);
    }

    [Fact]
    public async Task MustCorrectlyDecorateAsyncMethodsAndPropertiesForAdornmentThatHasStaticTypeChecks()
    {
        ITestInterface decorated
            = new TestImplementation
              {
                  DoubleProperty = 12d,
                  IntProperty = 22,
                  ObjectProperty = "AsyncTest"
              };

        const int multiplier = 11;

        ITestInterface decorator = new TestMultiplicationDecorator(
            multiplier: multiplier,
            decorated
        );
        Assert.Equal(expected: await decorated.GetIntAsync() * multiplier,
                     actual: await decorator.GetIntAsync());

        Assert.Equal(expected: await decorated.GetDoubleAsync() * multiplier,
                     actual: await decorator.GetDoubleAsync());

        Assert.Equal(expected: await decorated.GetObjectAsync(),
                     actual: await decorator.GetObjectAsync());
    }
    public interface ITestInterface
    {
        public int IntProperty { get; }
        public double DoubleProperty { get; }
        public object? ObjectProperty { get; }

        public event EventHandler Event;

        public void DoNothing()
        {
        }

        public int GetInt() => IntProperty;
        public object? GetObject() => ObjectProperty;

        public async Task<object?> GetObjectAsync()
        {
            await Task.Yield();
            return ObjectProperty;
        }

        public Task<int> GetIntAsync()
            => Task.FromResult(IntProperty);

        public Task<double> GetDoubleAsync()
            => Task.FromResult(DoubleProperty);
    }

    class TestImplementation : ITestInterface
    {
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public object? ObjectProperty { get; set; }

        public event EventHandler? Event;
    }
}

