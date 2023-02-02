using Test.Lexer;

namespace Test.Parser.Constructs;

public class InvocationParser : IParse<Invocation>
{
    public required Func<IParse<IExpression>> Expression { get; init; }

    public IResult<IParsed<Invocation>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [IdentifierToken func, BeginToken { C: '(' }, ..])
            return ts[0].Err<Invocation>();

        var argExpressions = new List<IExpression>();

        ts.RemoveRange(0, 2);
        while (true)
        {
            if (ts is [EndToken { C: ')' }, ..])
                return new Invocation(func.Text, argExpressions).Ok(ts.Skip(1));

            switch (Expression().Accept(ts))
            {
                case IOk<IParsed<IExpression>> ok:
                    argExpressions.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<IExpression>> err:
                    return err.To<IParsed<Invocation>>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ')' }, ..]:
                    return ts[0].Err<Invocation>();
            }
        }
    }
}