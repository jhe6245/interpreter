namespace Test.Parser;
public record Program(IEnumerable<IStatement> Statements);

public interface IStatement {}

public interface IExpression : IStatement {}

public record Evaluation(string Identifier) : IExpression;
public record Assignment(string Identifier, IExpression Expression) : IExpression;
public record Const(object Value) : IExpression;
public record Invocation(string Identifier, IEnumerable<IExpression> Arguments) : IExpression;
public record Lambda(IEnumerable<string> Parameters, IExpression Body) : IExpression;
public record UnaryOperator(string Operator, IExpression Arg) : IExpression;
public record BiOperator(string Operator, IExpression Left, IExpression Right) : IExpression;
public record ListInit(IEnumerable<IExpression> Items) : IExpression;

public record Initialization(string Identifier, IExpression Expression) : IStatement;
public record Conditional(IExpression Condition, IStatement Statement) : IStatement;
public record Iteration(string Iterator, IExpression Enumerable, IStatement Statement) : IStatement;
public record Block(IEnumerable<IStatement> Statements) : IStatement;
public record ExpressionBlock(IEnumerable<IStatement> Body, IExpression Return) : Block(Body.Append(Return)), IExpression;
