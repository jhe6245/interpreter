using Test.Lexer;

namespace Test.Parser.Constructs;

public class InitParser : IParse<Initialization>
{
    public required IParse<Assignment> Assignment { get; init; }

    public IResult<IParsed<Initialization>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        if (ts is [KeywordToken { Text: Lang.Keyword.DeclareVar }, ..])
        {
            return Assignment.Accept(ts.Skip(1))
                             .FlatMap(a => new Initialization(a.Result.Identifier, a.Result.Expression)
                                          .Ok(a.Remaining));
        }

        return ts[0].Err<Initialization>();
    }
}