using Test.Lexer;

namespace Test.Parser.Constructs;

public class IfParser : IParse<Conditional>
{
    public required Func<IParse<IExpression>> Expression { get; init; }
    public required Func<IParse<IStatement>> Stmt { get; init; }

    public IResult<IParsed<Conditional>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [KeywordToken { Text: "if" }, BeginToken { C: '(' }, ..])
            return ts[0].Err<Conditional>();

        ts.RemoveRange(0, 2);

        return Expression().Accept(ts).FlatMap(expr =>
        {
            var rem = expr.Remaining.ToList();
            if (rem is not [EndToken { C: ')' }, ..])
                return rem[0].Err<Conditional>();
            rem.RemoveAt(0);
            return Stmt().Accept(rem)
                         .FlatMap(stmt => new Conditional(expr.Result, stmt.Result).Ok(stmt.Remaining));
        });
    }
}