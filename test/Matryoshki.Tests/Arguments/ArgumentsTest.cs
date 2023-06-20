using System;
using System.Threading.Tasks;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.Arguments;

public class ArgumentsTest
{
    [Fact]
    public async Task MustGetArgumentsByExactType()
    {
        Matryoshka<ITestInterface>
            .With<ArgumentsTestingAdornment<int>>()
            .Name<IntArgumentsTestingDecorator>();

        var decorator = new IntArgumentsTestingDecorator(
            new TestImplementation());

        const double first = 0.34d;
        const int intValue = 11;
        const float third = 1.111f;
        const string fourth = "👋";

        await decorator.DoSomethingAsync(first, intValue, third, fourth);

        Assert.Equal(
            expected: new[] { intValue },
            actual: decorator.ValuesOfT_Δ);

        Assert.Equal(
            expected: new[] { new Argument<int>("second", intValue) },
            actual: decorator.ArgumentsOfT_Δ);
    }

    [Fact]
    public async Task MustGetArgumentsImplementingSpecifiedType()
    {
        Matryoshka<ITestInterface>
            .With<ArgumentsTestingAdornment<IFormattable>>()
            .Name<FormattableArgumentsTestingDecorator>();

        var decorator = new FormattableArgumentsTestingDecorator(
            new TestImplementation());

        const double first = 0.34d;
        const int second = 11;
        const float third = 1.111f;
        const string fourth = "👋";

        await decorator.DoSomethingAsync(first, second, third, fourth);

        Assert.Equal(
            expected: new IFormattable[] { first, second, third },
            actual: decorator.ValuesOfT_Δ);

        Assert.Equal(
            expected: new[]
                      {
                          new Argument<IFormattable>("first", first),
                          new Argument<IFormattable>("second", second),
                          new Argument<IFormattable>("third", third)
                      },
            actual: decorator.ArgumentsOfT_Δ);
    }

    [Fact]
    public async Task MustGetAllArgumentsByObjectType()
    {
        Matryoshka<ITestInterface>
            .With<ArgumentsTestingAdornment<object>>()
            .Name<ObjectArgumentsTestingDecorator>();

        var decorator = new ObjectArgumentsTestingDecorator(
            new TestImplementation());

        const double first = 0.34d;
        const int second = 11;
        const float third = 1.111f;
        const string fourth = "👋";

        await decorator.DoSomethingAsync(first, second, third, fourth);

        Assert.Equal(
            expected: new object[] { first, second, third, fourth },
            actual: decorator.ValuesOfT_Δ);

        Assert.Equal(
            expected: new[]
                      {
                          new Argument<object>("first", first),
                          new Argument<object>("second", second),
                          new Argument<object>("third", third),
                          new Argument<object>("pattern", fourth)
                      },
            actual: decorator.ArgumentsOfT_Δ);
    }

    [Fact]
    public async Task MustGetFirstArgumentOfType()
    {
        Matryoshka<ITestInterface>
            .With<ArgumentsTestingAdornment<string>>()
            .Name<StringArgumentsTestingDecorator>();

        var decorator = new StringArgumentsTestingDecorator(
            new TestImplementation());

        const double first = 0.34d;
        const int second = 11;
        const float third = 1.111f;
        const string fourth = "👋";

        await decorator.DoSomethingAsync(first, second, third, fourth);

        Assert.Equal(
            expected: fourth,
            actual: decorator.FirstValueOfT_Δ);

        Assert.Equal(
            expected: new Argument<string>("pattern", fourth),
            actual: decorator.FirstArgumentOfT_Δ);
    }

    [Fact]
    public async Task MustGetParameterNames()
    {
        var decorator = new StringArgumentsTestingDecorator(
            new TestImplementation());

        await decorator.DoSomethingAsync(default, default, default!, default!);

        Assert.Equal(
            expected: new[] { "first", "second", "third", "pattern" },
            actual: decorator.ParameterNames_Δ);
    }

    [Fact]
    public void MustGetPropertySetterValue()
    {
        var decorator = new StringArgumentsTestingDecorator(
            new TestImplementation());

        const bool expectedBool = true;
        decorator.BoolProp = expectedBool;

        Assert.Equal(
            expected: expectedBool,
            actual: decorator.SetterValue_Δ);
    }

    [Fact]
    public void MustGetIndexerSetterValue()
    {
        var decorator = new StringArgumentsTestingDecorator(
            new TestImplementation());

        const int expectedInt = 10000;
        decorator[100] = expectedInt;

        Assert.Equal(
            expected: expectedInt,
            actual: decorator.SetterValue_Δ);

    }

    public interface ITestInterface
    {
        public int this[int a] { get; set; }
        public bool BoolProp { get; set; }

        public ValueTask<double> DoSomethingAsync(double first, int second, IFormattable third, string pattern)
            => ValueTask.FromResult(first * second);

        public ValueTask<double> DoSomethingAsync(int overload)
            => ValueTask.FromResult(overload * 1d);
    }

    private record TestImplementation : ITestInterface
    {
        public bool BoolProp { get; set; }

        public int this[int a]
        {
            get => a;
            set { }
        }
    }
}