using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.Members;

public class MembersMetadataTest
{
    [Fact]
    public void MustProvideCorrectMetadataForIndexerGetter()
    {
        Matryoshka<ITestInterface>
            .With<MemberMetadataTestingAdornment>()
            .Name<MembersTestingDecorator>();

        var decorator = new MembersTestingDecorator(
            new TestImplementation());

        _ = decorator[0];

        Assert.True(decorator.IsProperty_Δ);
        Assert.True(decorator.IsGetter_Δ);
        Assert.False(decorator.IsMethod_Δ);
        Assert.False(decorator.IsSetter_Δ);
        Assert.Equal(
            expected: "this[]", 
            decorator.MemberName_Δ);
    }


    [Fact]
    public void MustProvideCorrectMetadataForIndexerSetter()
    {
        var decorator = new MembersTestingDecorator(
            new TestImplementation());

        decorator[0] = 1;

        Assert.True(decorator.IsProperty_Δ);
        Assert.False(decorator.IsGetter_Δ);
        Assert.False(decorator.IsMethod_Δ);
        Assert.True(decorator.IsSetter_Δ);
        Assert.Equal(
            expected: "this[]", 
            actual: decorator.MemberName_Δ);
    }

    [Fact]
    public void MustProvideCorrectMetadataForPropertyGetter()
    {

        var decorator = new MembersTestingDecorator(
            new TestImplementation());

        _ = decorator.BoolProp;

        Assert.True(decorator.IsProperty_Δ);
        Assert.True(decorator.IsGetter_Δ);
        Assert.False(decorator.IsMethod_Δ);
        Assert.False(decorator.IsSetter_Δ);
        Assert.Equal(
            expected: nameof(ITestInterface.BoolProp),
            actual: decorator.MemberName_Δ);
    }

    [Fact]
    public void MustProvideCorrectMetadataForPropertySetter()
    {
        var decorator = new MembersTestingDecorator(
            new TestImplementation());

        decorator.BoolProp = true;

        Assert.True(decorator.IsProperty_Δ);
        Assert.False(decorator.IsGetter_Δ);
        Assert.False(decorator.IsMethod_Δ);
        Assert.True(decorator.IsSetter_Δ);
        Assert.Equal(
            expected: nameof(ITestInterface.BoolProp),
            actual: decorator.MemberName_Δ);
    }

    [Fact]
    public void MustProvideCorrectMetadataForMethod()
    {
        var decorator = new MembersTestingDecorator(
            new TestImplementation());

        decorator.SomeMethod();

        Assert.False(decorator.IsProperty_Δ);
        Assert.False(decorator.IsGetter_Δ);
        Assert.False(decorator.IsSetter_Δ);
        Assert.True(decorator.IsMethod_Δ);
        Assert.Equal(
            expected: nameof(ITestInterface.SomeMethod),
            actual: decorator.MemberName_Δ);
    }

    public interface ITestInterface
    {
        public int this[int a] { get; set; }
        public bool BoolProp { get; set; }

        public void SomeMethod()
        {
        }
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