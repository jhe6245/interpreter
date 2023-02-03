using System.Diagnostics;
using Test.Lexer;

namespace Test.Parser;

public record ParsedResult<T>(T Result, IEnumerable<Token> Remaining) : IParsed<T>;

public static class Parsed
{
    public static IResult<IParsed<T>> Ok<T>(this T result, IEnumerable<Token> Remaining) =>
        Result.Ok(new ParsedResult<T>(result, Remaining));

    public static IResult<IParsed<T>> Err<T>(this Token t) =>
        Result.Err<IParsed<T>>(t);

    public static IResult<IParsed<T2>> Chain<T1, T2>(this IResult<IParsed<T1>> result, Func<T1, IList<Token>, IResult<IParsed<T2>>> f)
        => result.FlatMap(r => f(r.Result, r.Remaining.ToList()));
}

public interface IParsed<out T>
{
    T Result { get; }
    IEnumerable<Token> Remaining { get; }
}

public interface IResult<out T>
{
}

public interface IErr<out T> : IResult<T>
{
    StackTrace Trace { get; }
    Token? Error { get; }

    IErr<T2> To<T2>();
}

public interface IOk<out T> : IResult<T>
{
    T Result { get; }
}

public static class Result
{
    private record OkResult<T>(T Result) : IOk<T>;

    private record ErrResult<T>(StackTrace Trace, Token? Error = null) : IErr<T>
    {
        public IErr<T2> To<T2>() => new ErrResult<T2>(Trace, Error);
    }

    public static IResult<T> Ok<T>(T result) => new OkResult<T>(result);

    public static IResult<T> Err<T>(Token? error = null) => new ErrResult<T>(new StackTrace(), error);

    public static IResult<T2> Map<T1, T2>(this IResult<T1> result, Func<T1, T2> f) =>
        result.FlatMap(r => new OkResult<T2>(f(r)));
    
    public static IResult<T2> FlatMap<T1, T2>(this IResult<T1> result, Func<T1, IResult<T2>> f) => result switch
    {
        IOk<T1> ok => f(ok.Result),
        IErr<T1> err => new ErrResult<T2>(err.Trace, err.Error),
        _ => throw new ArgumentException(null, nameof(result))
    };
}