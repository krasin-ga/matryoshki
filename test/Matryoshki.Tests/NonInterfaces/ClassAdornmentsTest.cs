using System;
using System.Reflection;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.NonInterfaces;

public class ClassAdornmentsTest
{
    [Theory]
    [InlineData(nameof(TestClass.VirtualMethod), true)]
    [InlineData(nameof(TestClass.BaseAbstractMethod), true)]
    [InlineData(nameof(TestClass.SealedBaseAbstractMethod), false)]
    [InlineData(nameof(TestClass.NonVirtualMethod), false)]
    public void MustGenerateDecoratorsForClassesWithVirtualOrAbstractMethods(
        string methodName,
        bool mustBeDecorated)
    {
        #pragma warning disable MatryoshkiSourceGeneratorNonInterface

        var matryoshkaType = Matryoshka<TestClass>
                             .With<SimpleAdornment>()
                             .Name<SimpleDecorator>();

        #pragma warning restore MatryoshkiSourceGeneratorNonInterface

        var decorator = new SimpleDecorator(
            new TestClass());

        var method = decorator.GetType().GetMethod(
            methodName,
            BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.FlattenHierarchy)!;

        method.Invoke(
            decorator,
            Array.Empty<object>());

        Assert.Equal(expected: mustBeDecorated, decorator.WasExecuted_Δ);
        Assert.Equal(expected: typeof(TestClass), matryoshkaType.Target);
        Assert.Equal(expected: typeof(SimpleDecorator), matryoshkaType.Type);
    }

    public class TestClass : BaseClass
    {
        public void NonVirtualMethod()
        {
        }

        public virtual void VirtualMethod()
        {
        }

        public override void BaseAbstractMethod()
        {
        }

        public sealed override void SealedBaseAbstractMethod()
        {
        }
    }

    public abstract class BaseClass
    {
        public abstract void BaseAbstractMethod();
        public abstract void SealedBaseAbstractMethod();
    }
}