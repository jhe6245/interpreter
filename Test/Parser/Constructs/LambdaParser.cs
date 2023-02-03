using Test.Lexer;

namespace Test.Parser.Constructs;

public class LambdaParser : IParse<Lambda>
{
    public required IParse<Block> Block { get; init; }
    public required Func<IParse<IExpression>> Expression { get; init; }

    public IResult<IParsed<Lambda>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is [IdentifierToken { Text: var single }, ArrowToken, ..])
        {
            if (Expression().Accept(ts.Skip(2))
                            .FlatMap(e => new ExpressionLambda(new[] { single }, e.Result).Ok(e.Remaining))
                            is IOk<IParsed<ExpressionLambda>> ok)
            {
                return ok;
            }

            return Block.Accept(ts.Skip(2))

                          .FlatMap(b => new BlockLambda(new[] { single }, b.Result).Ok(b.Remaining));
        }


        if (ts is not [BeginToken { C: '(' }, ..])
            return ts[0].Err<ExpressionLambda>();

        var parameters = new List<string>();

        ts.RemoveAt(0);
        while (true)
        {
            switch (ts)
            {
                case [EndToken { C: ')' }, ArrowToken, ..]:
                    if (Expression().Accept(ts.Skip(2))
                                    .FlatMap(e => new ExpressionLambda(parameters, e.Result).Ok(e.Remaining))
                        is IOk<IParsed<ExpressionLambda>> ok)
                    {
                        return ok;
                    }

                    return Block.Accept(ts.Skip(2))

                                  .FlatMap(b => new BlockLambda(parameters, b.Result).Ok(b.Remaining));
                case [IdentifierToken { Text: var parameter }, ..]:
                    parameters.Add(parameter);
                    ts.RemoveAt(0);
                    break;
                default:
                    return ts[0].Err<ExpressionLambda>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ')' }, ArrowToken, ..]:
                    return ts[0].Err<ExpressionLambda>();
            }
        }
    }
}