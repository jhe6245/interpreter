using Test.Lexer;

namespace Test.Parser.Constructs;

public class AssignmentParser : IParse<Assignment>
{
    public required Func<IParse<IExpression>> Expression { get; init; }

    public IResult<IParsed<Assignment>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();
        if (ts is [IdentifierToken assignVar, OperatorToken { Text: ":=" }, ..])
        {
            return Expression().Accept(ts.Skip(2))
                               .FlatMap(e => new Assignment(assignVar.Text, e.Result).Ok(e.Remaining));
        }

        return ts[0].Err<Assignment>();
    }
}