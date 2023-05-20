using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.InterfaceExtraction;

public class InterfaceExtractionTest
{
    static InterfaceExtractionTest()
    {
        From<TestClass>.ExtractInterface<ITestClassInterface>();
    }

    [Fact]
    public void MustBeAbleToGenerateDecoratorsForExtractedInterfaces()
    {
        Decorate<ITestClassInterface>
            .With<SimpleAdornment>()
            .Name<TestClassInterfaceDecorator>();

        var decorator = new TestClassInterfaceDecorator(
            new ITestClassInterface.Adapter(new TestClass()));

        decorator.DoSomething(default, default);

        Assert.True(decorator.WasExecuted_Δ);
    }

    public class TestClass
    {
        public int this[int a, int b]
        {
            get => 1;
            set { }
        }

        public int TestBoolProperty { get; set; }

        public void DoSomething(int a, int b)
        {
        }
    }
}