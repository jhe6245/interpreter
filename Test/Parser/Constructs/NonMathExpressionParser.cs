using Test.Lexer;

namespace Test.Parser.Constructs;

public class NonMathExpressionParser : IParse<Expression> // called from math parser
{
    public required IParse<Invocation> Invocation { get; init; }
    public required IParse<ListInit> List { get; init; }
    public required IParse<Assignment> Assignment { get; init; }

    public IResult<IParsed<Expression>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts switch
        {
            [KeywordToken { Text: Lang.Keyword.True }, ..] => new Const(true).Ok(ts.Skip(1)),
            [KeywordToken { Text: Lang.Keyword.False }, ..] => new Const(false).Ok(ts.Skip(1)),

            [StringToken st, ..] => new Const(st.Text).Ok(ts.Skip(1)),
            [NumericToken nt, ..] => new Const(nt.Text).Ok(ts.Skip(1)),

            _ when Assignment.Accept(ts) is IOk<IParsed<Assignment>> ok => ok,
            _ when Invocation.Accept(ts) is IOk<IParsed<Invocation>> ok => ok,
            _ when List.Accept(ts) is IOk<IParsed<ListInit>> ok => ok,

            [IdentifierToken lambdaArg, ArrowToken, ..]
                => Accept(ts.Skip(2))
                    .FlatMap(e => new Lambda(new[] { lambdaArg.Text }, e.Result).Ok(e.Remaining)),

            [IdentifierToken evalVar, ..]
                => new Evaluation(evalVar.Text).Ok(ts.Skip(1)),

            _ => ts.First().Err<Expression>()
        };
    }
}