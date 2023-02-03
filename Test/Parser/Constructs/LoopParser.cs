using Test.Lexer;

namespace Test.Parser.Constructs;

public class LoopParser : IParse<Loop>
{
    public required IParse<IExpression> Expression { get; init; }
    public required Func<IParse<IStatement>> Statement { get; init; }

    public IResult<IParsed<Loop>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts is [KeywordToken { Text: Lang.Keyword.Loop }, BeginToken { C: '(' }, ..]
            ? Expression.Accept(ts.Skip(2)).Chain(
                (cond, rem) =>
                    rem is [EndToken { C: ')' }, ..]
                        ? Statement().Accept(rem.Skip(1))
                                   .FlatMap(body => new Loop(cond, body.Result)
                                                .Ok(body.Remaining))
                        : ts[0].Err<Loop>())
            : ts[0].Err<Loop>();
    }
}