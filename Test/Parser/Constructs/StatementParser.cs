using Test.Lexer;

namespace Test.Parser.Constructs;

public class StatementParser : IParse<Statement>
{
    public required IParse<Expression> Expression { get; init; }
    public required IParse<Initialization> Initialization { get; init; }
    public required IParse<Conditional> Conditional { get; init; }
    public required IParse<Iteration> Iteration { get; init; }

    public IResult<IParsed<Statement>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [BeginToken { Text: Lang.BlockBegin }, ..])
        {
            return ts switch
            {
                _ when Initialization.Accept(ts) is IOk<IParsed<Initialization>> ok => ok,
                _ when Conditional.Accept(ts) is IOk<IParsed<Conditional>> ok => ok,
                _ when Iteration.Accept(ts) is IOk<IParsed<Iteration>> ok => ok,

                _ => Expression.Accept(ts)
            };
        }

        ts.RemoveAt(0);
        var blockStmts = new List<Statement>();

        while (ts is not [EndToken { Text: Lang.BlockEnd }, ..])
        {
            switch (Accept(ts))
            {
                case IOk<IParsed<Statement>> ok:
                    blockStmts.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;

                case IErr<IParsed<Statement>> err:
                    return err;
            }
        }

        return new Block(blockStmts).Ok(ts.Skip(1));
    }
}