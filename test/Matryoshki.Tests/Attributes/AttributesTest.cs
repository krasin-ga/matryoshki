using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.Attributes;

public class AttributesTest
{
    [Fact]
    public void MustCopyAttributesFromAdornmentsToDecorators()
    {
        Decorate<ICollection>.With<SimpleAdornment>().Name<SimpleCollectionDecorator>();

        Assert.NotNull(
            typeof(SimpleCollectionDecorator)
                .GetCustomAttribute<ExcludeFromCodeCoverageAttribute>()
        );
    }
}