using Test.Lexer;

namespace Test.Parser.Constructs;

public class ConditionalParser : IParse<Conditional>
{
    public required Func<IParse<IExpression>> Expression { get; init; }
    public required Func<IParse<IStatement>> Stmt { get; init; }

    public IResult<IParsed<Conditional>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [KeywordToken { Text: Lang.Keyword.If }, BeginToken { C: '(' }, ..])
            return ts[0].Err<SingleConditional>();

        ts.RemoveRange(0, 2);

        return Expression().Accept(ts).Chain((expr, rem1) =>
        {
            if (rem1 is not [EndToken { C: ')' }, ..])
                return rem1[0].Err<SingleConditional>();

            return Stmt().Accept(rem1.Skip(1))
                         .Chain((ifBody, rem2) =>
                         {
                             if (rem2 is [KeywordToken { Text: Lang.Keyword.Else }, ..])
                             {
                                 return Stmt().Accept(rem2.Skip(1))
                                              .FlatMap(elseBody =>
                                                           new DoubleConditional(expr, ifBody, elseBody.Result)
                                                               .Ok(elseBody.Remaining));
                             }

                             return new SingleConditional(expr, ifBody).Ok<Conditional>(rem2);
                         });
        });
    }
}