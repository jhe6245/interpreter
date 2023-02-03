using Test.Lexer;

namespace Test.Parser.Constructs;

public class ReturnParser : IParse<Return>
{
    public required IParse<IExpression> Expression { get; init; }

    public IResult<IParsed<Return>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts is [KeywordToken { Text: Lang.Keyword.Return }, ..]
            ? Expression.Accept(ts.Skip(1)).FlatMap(e => new Return(e.Result).Ok(e.Remaining))
            : ts[0].Err<Return>();
    }
}