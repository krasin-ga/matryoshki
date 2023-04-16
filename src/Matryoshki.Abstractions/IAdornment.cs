namespace Matryoshki.Abstractions;

public interface IAdornment
{
    public TResult MethodTemplate<TResult>(Call<TResult> call);

    public Task<TResult> AsyncMethodTemplate<TResult>(Call<TResult> call)
        => Task.FromResult<TResult>(default!);
}
