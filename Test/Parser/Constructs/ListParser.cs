using Test.Lexer;

namespace Test.Parser.Constructs;

public class ListParser : IParse<ListInit>
{
    public required Func<IParse<Expression>> Expression { get; init; }

    public IResult<IParsed<ListInit>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [BeginToken { C: '[' }, ..])
            return ts[0].Err<ListInit>();

        var expressions = new List<Expression>();

        ts.RemoveAt(0);
        while (true)
        {
            if (ts is [EndToken { C: ']' }, ..])
                return new ListInit(expressions).Ok(ts.Skip(1));

            switch (Expression().Accept(ts))
            {
                case IOk<IParsed<Expression>> ok:
                    expressions.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<Expression>> err:
                    return err.To<IParsed<ListInit>>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ']' }, ..]:
                    return ts[0].Err<ListInit>();
            }
        }
    }
}