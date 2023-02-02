using Test.Lexer;

namespace Test.Parser;

public class Parser : IParse<Program>
{
    public required IParse<Statement> Stmt { get; init; }

    public IResult<IParsed<Program>> Accept(IEnumerable<Token> tokens)
    {
        List<Statement> statements = new();

        var l = tokens.ToList();

        while (l.Count > 0)
        {
            switch (Stmt.Accept(l))
            {
                case IOk<IParsed<Statement>> ok:
                    statements.Add(ok.Result.Result);
                    l = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<Statement>> err:
                    return err.To<IParsed<Program>>();
            }
        }

        return new Program(statements).Ok(l);
    }
}