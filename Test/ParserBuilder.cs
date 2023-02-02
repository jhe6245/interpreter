using Test.Parser;
using Test.Parser.Constructs;

namespace Test;

public class ParserBuilder
{
    private readonly IParse<IExpression> expression;
    private readonly IParse<IStatement> stmt;

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
        var block = new BlockParser
        {
            Statement = () => stmt!
        };

        expression = new ExpressionParser
        {
            ValueExpression = new NonMathExpressionParser
            {
                Lambda = new LambdaParser
                {
                    Expression = () => expression!
                },
                List = new ListParser
                {
                    Expression = () => expression!
                },
                Assignment = assignment,
                Invocation = new InvocationParser
                {
                    Expression = () => expression!
                },
                Block = block
            }
        };

        stmt = new StatementParser
        {
            Conditional = conditional,
            Block = block,
            Initialization = new InitParser { Assignment = assignment },
            Expression = expression,
            Iteration = new LoopParser { Expression = expression, Stmt = () => stmt! }
        };
    }

    public IParse<Parser.Program> Build() =>
        new Parser.Parser
        {
            Stmt = stmt
        };
}