namespace Test.Parser;
public record Program(IEnumerable<Statement> Statements);

public abstract record Statement;

public abstract record Expression : Statement;

public record Evaluation(string Identifier) : Expression;
public record Assignment(string Identifier, Expression Expression) : Expression;
public record Const(object Value) : Expression;
public record Invocation(string Identifier, IEnumerable<Expression> Arguments) : Expression;
public record Lambda(IEnumerable<string> Parameters, Expression Body) : Expression;
public record UnaryOperator(string Operator, Expression Arg) : Expression;
public record BiOperator(string Operator, Expression Left, Expression Right) : Expression;
public record ListInit(IEnumerable<Expression> Items) : Expression;

public record Initialization(string Identifier, Expression Expression) : Statement;
public record Conditional(Expression Condition, Statement Statement) : Statement;
public record Iteration(string Iterator, Expression Enumerable, Statement Statement) : Statement;
public record Block(IEnumerable<Statement> Statements) : Statement;
