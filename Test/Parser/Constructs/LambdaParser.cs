using Test.Lexer;

namespace Test.Parser.Constructs;

public class LambdaParser : IParse<Lambda>
{
    public required Func<IParse<IExpression>> Expression { get; init; }

    public IResult<IParsed<Lambda>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is [IdentifierToken { Text: var single }, ArrowToken, ..])
        {
            return Expression().Accept(ts.Skip(2))
                               .FlatMap(e => new Lambda(new[] { single }, e.Result).Ok(e.Remaining));
        }


        if (ts is not [BeginToken { C: '(' }, ..])
            return ts[0].Err<Lambda>();

        var parameters = new List<string>();

        ts.RemoveAt(0);
        while (true)
        {
            switch (ts)
            {
                case [EndToken { C: ')' }, ArrowToken, ..]:
                    return Expression().Accept(ts.Skip(2))
                                       .FlatMap(e => new Lambda(parameters, e.Result).Ok(e.Remaining));
                case [IdentifierToken { Text: var parameter }, ..]:
                    parameters.Add(parameter);
                    ts.RemoveAt(0);
                    break;
                default:
                    return ts[0].Err<Lambda>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ')' }, ArrowToken, ..]:
                    return ts[0].Err<Lambda>();
            }
        }
    }
}