using Test.Lexer;

namespace Test.Parser.Constructs;

public class LoopParser : IParse<Iteration>
{
    public required IParse<Expression> Expression { get; init; }
    public required Func<IParse<Statement>> Stmt { get; init; }

    public IResult<IParsed<Iteration>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not
            [
                KeywordToken { Text: Lang.Keyword.For }, BeginToken { Text: Lang.ControlStructureArgBegin },
                KeywordToken { Text: Lang.Keyword.DeclareVar }, IdentifierToken id, OperatorToken { Text: ":" }, ..
            ]) return ts[0].Err<Iteration>();
        ts.RemoveRange(0, 5);

        return Expression.Accept(ts).FlatMap(expr =>
        {
            ts = expr.Remaining.ToList();

            if (ts is not [EndToken { C: ')' }, ..])
                return ts[0].Err<Iteration>();

            ts.RemoveAt(0);
            return Stmt().Accept(ts)
                         .FlatMap(stmt => new Iteration(id.Text, expr.Result, stmt.Result).Ok(stmt.Remaining));
        });
    }
}