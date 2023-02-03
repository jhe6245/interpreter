namespace Test.Parser;
public record Program(IEnumerable<IStatement> Statements);

public interface IStatement {}

public interface IExpression : IStatement {}

public record Evaluation(string Identifier) : IExpression;
public record Assignment(string Identifier, IExpression Expression) : IExpression;
public record Const(object Value) : IExpression;
public record Invocation(string Identifier, IEnumerable<IExpression> Arguments) : IExpression;

public abstract record Lambda(IEnumerable<string> Parameters) : IExpression;
public record ExpressionLambda(IEnumerable<string> Parameters, IExpression Body) : Lambda(Parameters);
public record BlockLambda(IEnumerable<string> Parameters, Block Body) : Lambda(Parameters);

public record UnaryOperator(string Operator, IExpression Arg) : IExpression;
public record BiOperator(string Operator, IExpression Left, IExpression Right) : IExpression;
public record ListInit(IEnumerable<IExpression> Items) : IExpression;

public record Initialization(string Identifier, IExpression Expression) : IStatement;

public abstract record Conditional(IExpression Condition) : IStatement;
public record SingleConditional(IExpression Condition, IStatement Statement) : Conditional(Condition);
public record DoubleConditional(IExpression Condition, IStatement True, IStatement False) : Conditional(Condition);
public record Iteration(string Iterator, IExpression Enumerable, IStatement Statement) : IStatement;

public record Block(IEnumerable<IStatement> Statements) : IStatement;
public record ExpressionBlock(IEnumerable<IStatement> Body, IExpression Return) : Block(Body.Append(Return)), IExpression;

public record Return(IExpression Expression) : IStatement;
