using Test.Lexer;

namespace Test.Parser;

public interface IParse<out T>
{
    public IResult<IParsed<T>> Accept(IEnumerable<Token> tokens);
}