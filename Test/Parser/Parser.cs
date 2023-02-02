using Test.Lexer;

namespace Test.Parser;

public class Parser : IParse<Program>
{
    public required IParse<IStatement> Stmt { get; init; }

    public IResult<IParsed<Program>> Accept(IEnumerable<Token> tokens)
    {
        List<IStatement> statements = new();

        var l = tokens.ToList();

        while (l.Count > 0)
        {
            switch (Stmt.Accept(l))
            {
                case IOk<IParsed<IStatement>> ok:
                    statements.Add(ok.Result.Result);
                    l = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<IStatement>> err:
                    return err.To<IParsed<Program>>();
            }
        }

        return new Program(statements).Ok(l);
    }
}