using Test.Lexer;

namespace Test.Parser.Constructs;

public class BlockParser : IParse<Block>
{
    public required Func<IParse<IStatement>> Statement { get; init; }

    public IResult<IParsed<Block>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [BeginToken { Text: Lang.BlockBegin }, ..])
            return ts[0].Err<Block>();

        ts.RemoveAt(0);
        var statements = new List<IStatement>();

        while (ts is not [EndToken { Text: Lang.BlockEnd }, ..])
        {
            switch (Statement().Accept(ts))
            {
                case IOk<IParsed<IStatement>> ok:
                    statements.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;

                case IErr<IParsed<IStatement>> err:
                    return err.To<IParsed<Block>>();
            }
        }

        if (statements[^1] is IExpression expr)
            return new ExpressionBlock(statements.Take(statements.Count - 1), expr).Ok(ts.Skip(1));

        return new Block(statements).Ok(ts.Skip(1));
    }
}