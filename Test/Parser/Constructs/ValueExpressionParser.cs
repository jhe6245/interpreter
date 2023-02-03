using Test.Lexer;

namespace Test.Parser.Constructs;

public class ValueExpressionParser : IParse<IExpression>
{
    public required IParse<Lambda> Lambda { get; set; }
    public required IParse<Invocation> Invocation { get; init; }
    public required IParse<ListInit> List { get; init; }
    public required IParse<Assignment> Assignment { get; init; }
    public required IParse<Block> Block { get; init; }

    public IResult<IParsed<IExpression>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts switch
        {
            [KeywordToken { Text: Lang.Keyword.True }, ..] => new Const(true).Ok(ts.Skip(1)),
            [KeywordToken { Text: Lang.Keyword.False }, ..] => new Const(false).Ok(ts.Skip(1)),

            [StringToken st, ..] => new Const(st.Text).Ok(ts.Skip(1)),
            [NumericToken nt, ..] => new Const(nt.Text).Ok(ts.Skip(1)),

            _ when Lambda.Accept(ts) is IOk<IParsed<Lambda>> ok => ok,
            _ when Block.Accept(ts) is IOk<IParsed<ExpressionBlock>> ok => ok,
            _ when Assignment.Accept(ts) is IOk<IParsed<Assignment>> ok => ok,
            _ when Invocation.Accept(ts) is IOk<IParsed<Invocation>> ok => ok,
            _ when List.Accept(ts) is IOk<IParsed<ListInit>> ok => ok,

            [IdentifierToken evalVar, ..]
                => new Evaluation(evalVar.Text).Ok(ts.Skip(1)),

            _ => ts.First().Err<IExpression>()
        };
    }
}