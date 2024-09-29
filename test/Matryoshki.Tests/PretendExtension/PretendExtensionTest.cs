using System;
using System.Collections.Generic;
using Matryoshki.Abstractions;
using Xunit;

namespace Matryoshki.Tests.Pretend;

public class PretendExtensionTest
{
    [Fact]
    public void MustRemoveCallsToPretendMethodFromTemplate()
    {
        Decorate<IList<string>>.With<AdornmentWithPretendMethodInTemplate>().Name<TestListDecorator>();

        var decoratedList = new TestListDecorator(new List<string>()) { "test_string" };

        Assert.Equal(
            "decorated(test_string)",
            decoratedList[0]);

        Assert.Equal(100, decoratedList.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => decoratedList[1]);
    }

    private class AdornmentWithPretendMethodInTemplate : IAdornment
    {
        public TResult MethodTemplate<TResult>(Call<TResult> call)
        {
            _ = this.Pretend<object>();
            _ = Math.E.Pretend<double>();
            _ = Math.Max(1, 2.Pretend<int>()).Pretend<double>();
            _ = call.Recipient;

            if (typeof(TResult) == typeof(int))
            {
                return (call.Forward().Pretend<int>() * 100).Pretend<TResult>();
            }
            else if (typeof(TResult) == typeof(string))
            {
                var result = call.Forward().Pretend<string>();
                return $"decorated({result})".Pretend<TResult>();
            }
            else
            {
                return call.Forward();
            }
        }
    }
}