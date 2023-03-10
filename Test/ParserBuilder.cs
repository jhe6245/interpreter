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
        var conditional = new ConditionalParser
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
            ValueExpression = new ValueExpressionParser
            {
                Lambda = new LambdaParser
                {
                    Block = block,
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
            Return = new ReturnParser { Expression = expression },
            Conditional = conditional,
            Block = block,
            Initialization = new InitParser { Assignment = assignment },
            Expression = expression,
            Iteration = new IterationParser { Expression = expression, Stmt = () => stmt! },
            Loop = new LoopParser { Expression = expression, Statement = () => stmt! }
        };
    }

    public IParse<Parser.Program> Build() =>
        new Parser.Parser
        {
            Stmt = stmt
        };
}