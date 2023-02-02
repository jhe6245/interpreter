using Test.Parser;
using Test.Parser.Constructs;

namespace Test;

public class ParserBuilder
{
    private readonly IParse<Statement> stmt;
    private readonly IParse<Expression> expression;

    public ParserBuilder()
    {
        var assignment = new AssignmentParser
        {
            Expression = () => expression!
        };
        var conditional = new IfParser
        {
            Expression = () => expression!,
            Stmt = () => stmt!
        };

        expression = new ExpressionParser
        {
            ValueExpression = new NonMathExpressionParser
            {
                List = new ListParser
                {
                    Expression = () => expression!,
                },
                Assignment = assignment,
                Invocation = new InvocationParser
                {
                    Expression = () => expression!,
                }
            }
        }; 
        
        stmt = new StatementParser
        {
            Conditional = conditional,
            Initialization = new InitParser { Assignment = assignment },
            Expression = expression,
            Iteration = new LoopParser { Expression = expression, Stmt = () => stmt! }
        };
    }

    public IParse<Parser.Program> Build()
    {
        return new Parser.Parser
        {
            Stmt = stmt
        };
    }
}