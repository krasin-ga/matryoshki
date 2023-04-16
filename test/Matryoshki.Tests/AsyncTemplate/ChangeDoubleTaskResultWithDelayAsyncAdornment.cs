using System.Threading.Tasks;
using Matryoshki.Abstractions;

namespace Matryoshki.Tests.AsyncTemplate;

public class ChangeDoubleTaskResultWithDelayAsyncAdornment : IAdornment
{
    private readonly double _result;

    public ChangeDoubleTaskResultWithDelayAsyncAdornment(double result)
    {
        _result = result;
    }

    public TResult MethodTemplate<TResult>(Call<TResult> call)
    {
        return call.Forward();
    }

    public async Task<TResult> AsyncMethodTemplate<TResult>(Call<TResult> call)
    {
        if (typeof(TResult) == typeof(double))
            await Task.Delay(1000);

        if (typeof(TResult) == typeof(double))
            return _result.Pretend<TResult>();

        return await call.ForwardAsync();
    }
}