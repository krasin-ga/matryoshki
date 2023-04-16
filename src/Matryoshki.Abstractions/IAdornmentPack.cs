namespace Matryoshki.Abstractions;

public interface INesting
{
}

public interface INesting<T1> : INesting
    where T1 : IAdornment
{
}

public interface INesting<T1, T2> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
{
}

public interface INesting<T1, T2, T3> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5, T6> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
    where T6 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5, T6, T7> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
    where T6 : IAdornment
    where T7 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5, T6, T7, T8> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
    where T6 : IAdornment
    where T7 : IAdornment
    where T8 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5, T6, T7, T8, T9> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
    where T6 : IAdornment
    where T7 : IAdornment
    where T8 : IAdornment
    where T9 : IAdornment
{
}

public interface INesting<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : INesting
    where T1 : IAdornment
    where T2 : IAdornment
    where T3 : IAdornment
    where T4 : IAdornment
    where T5 : IAdornment
    where T6 : IAdornment
    where T7 : IAdornment
    where T8 : IAdornment
    where T9 : IAdornment
    where T10 : IAdornment
{
}