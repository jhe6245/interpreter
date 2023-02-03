using Test.Lexer;

namespace Test.Parser.Constructs;

public class StatementParser : IParse<IStatement>
{
    public required IParse<Return> Return { get; init; }
    public required IParse<Block> Block { get; init; }
    public required IParse<Initialization> Initialization { get; init; }
    public required IParse<Conditional> Conditional { get; init; }
    public required IParse<Iteration> Iteration { get; init; }
    public required IParse<Loop> Loop { get; init; }
    public required IParse<IExpression> Expression { get; init; }

    public IResult<IParsed<IStatement>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        return ts switch
        {
            _ when Return.Accept(ts) is IOk<IParsed<Return>> ok => ok,
            _ when Block.Accept(ts) is IOk<IParsed<Block>> ok => ok,
            _ when Initialization.Accept(ts) is IOk<IParsed<Initialization>> ok => ok,
            _ when Conditional.Accept(ts) is IOk<IParsed<Conditional>> ok => ok,
            _ when Loop.Accept(ts) is IOk<IParsed<Loop>> ok => ok,
            _ when Iteration.Accept(ts) is IOk<IParsed<Iteration>> ok => ok,

            _ => Expression.Accept(ts)
        };
    }
}