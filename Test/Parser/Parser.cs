using Test.Lexer;

namespace Test.Parser;

public class Parser : IParse<Program>
{
    public required IParse<Statement> Stmt { get; init; }

    public IResult<IParsed<Program>> Accept(IEnumerable<Token> tokens)
    {
        List<Statement> statements = new();

        var l = tokens.ToList();

        while (l.Count > 0)
        {
            switch (Stmt.Accept(l))
            {
                case IOk<IParsed<Statement>> ok:
                    statements.Add(ok.Result.Result);
                    l = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<Statement>> err:
                    return err.To<IParsed<Program>>();
            }
        }

        return new Program(statements).Ok(l);
    }
}

public class StatementParser : IParse<Statement>
{
    public required IParse<Expression> Expression { get; init; }
    public required IParse<Initialization> Initialization { get; init; }
    public required IParse<Conditional> Conditional { get; init; }
    public required IParse<Iteration> Iteration { get; init; }

    public IResult<IParsed<Statement>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [BeginToken { Text: Lang.BlockBegin }, ..])
        {
            return ts switch
            {
                _ when Initialization.Accept(ts) is IOk<IParsed<Initialization>> ok => ok,
                _ when Conditional.Accept(ts) is IOk<IParsed<Conditional>> ok => ok,
                _ when Iteration.Accept(ts) is IOk<IParsed<Iteration>> ok => ok,

                _ => Expression.Accept(ts)
            };
        }

        ts.RemoveAt(0);
        var blockStmts = new List<Statement>();

        while (ts is not [EndToken { Text: Lang.BlockEnd }, ..])
        {
            switch (Accept(ts))
            {
                case IOk<IParsed<Statement>> ok:
                    blockStmts.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;

                case IErr<IParsed<Statement>> err:
                    return err;
            }
        }

        return new Block(blockStmts).Ok(ts.Skip(1));
    }
}

public class AssignmentParser : IParse<Assignment>
{
    public required Func<IParse<Expression>> Expression { get; init; }

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

public class NonMathExpressionParser : IParse<Expression>
{
    public required IParse<Invocation> Invocation { get; init; }
    public required IParse<ListInit> List { get; init; }
    public required IParse<Assignment> Assignment { get; init; }

    public IResult<IParsed<Expression>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts switch
        {
            [KeywordToken { Text: Lang.Keyword.True }, ..] => new Const(true).Ok(ts.Skip(1)),
            [KeywordToken { Text: Lang.Keyword.False }, ..] => new Const(false).Ok(ts.Skip(1)),

            [StringToken st, ..] => new Const(st.Text).Ok(ts.Skip(1)),
            [NumericToken nt, ..] => new Const(nt.Text).Ok(ts.Skip(1)),

            _ when Assignment.Accept(ts) is IOk<IParsed<Assignment>> ok => ok,
            _ when Invocation.Accept(ts) is IOk<IParsed<Invocation>> ok => ok,
            _ when List.Accept(ts) is IOk<IParsed<ListInit>> ok => ok,

            [IdentifierToken lambdaArg, ArrowToken, ..]
                => Accept(ts.Skip(2))
                    .FlatMap(e => new Lambda(new[] { lambdaArg.Text }, e.Result).Ok(e.Remaining)),

            [IdentifierToken evalVar, ..]
                => new Evaluation(evalVar.Text).Ok(ts.Skip(1)),

            _ => ts.First().Err<Expression>()
        };
    }
}

public class ListParser : IParse<ListInit>
{
    public required Func<IParse<Expression>> Expression { get; init; }

    public IResult<IParsed<ListInit>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [BeginToken { C: '[' }, ..])
            return ts[0].Err<ListInit>();

        var expressions = new List<Expression>();

        ts.RemoveAt(0);
        while (true)
        {
            if (ts is [EndToken { C: ']' }, ..])
                return new ListInit(expressions).Ok(ts.Skip(1));

            switch (Expression().Accept(ts))
            {
                case IOk<IParsed<Expression>> ok:
                    expressions.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<Expression>> err:
                    return err.To<IParsed<ListInit>>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ']' }, ..]:
                    return ts[0].Err<ListInit>();
            }
        }
    }
}

public class InvocationParser : IParse<Invocation>
{
    public required Func<IParse<Expression>> Expression { get; init; }

    public IResult<IParsed<Invocation>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [IdentifierToken func, BeginToken { C: '(' }, ..])
            return ts[0].Err<Invocation>();

        var argExpressions = new List<Expression>();

        ts.RemoveRange(0, 2);
        while (true)
        {
            if (ts is [EndToken { C: ')' }, ..])
                return new Invocation(func.Text, argExpressions).Ok(ts.Skip(1));

            switch (Expression().Accept(ts))
            {
                case IOk<IParsed<Expression>> ok:
                    argExpressions.Add(ok.Result.Result);
                    ts = ok.Result.Remaining.ToList();
                    break;
                case IErr<IParsed<Expression>> err:
                    return err.To<IParsed<Invocation>>();
            }

            switch (ts)
            {
                case [ListSepToken, ..]:
                    ts.RemoveAt(0);
                    break;
                case not [EndToken { C: ')' }, ..]:
                    return ts[0].Err<Invocation>();
            }
        }
    }
}

public class IfParser : IParse<Conditional>
{
    public required Func<IParse<Expression>> Expression { get; init; }
    public required Func<IParse<Statement>> Stmt { get; init; }

    public IResult<IParsed<Conditional>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not [KeywordToken { Text: "if" }, BeginToken { C: '(' }, ..])
            return ts[0].Err<Conditional>();

        ts.RemoveRange(0, 2);

        return Expression().Accept(ts).FlatMap(expr =>
        {
            var rem = expr.Remaining.ToList();
            if (rem is not [EndToken { C: ')' }, ..])
                return rem[0].Err<Conditional>();
            rem.RemoveAt(0);
            return Stmt().Accept(rem)
                         .FlatMap(stmt => new Conditional(expr.Result, stmt.Result).Ok(stmt.Remaining));
        });
    }
}

public class LoopParser : IParse<Iteration>
{
    public required IParse<Expression> Expression { get; init; }
    public required Func<IParse<Statement>> Stmt { get; init; }

    public IResult<IParsed<Iteration>> Accept(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToList();

        if (ts is not
            [
                KeywordToken { Text: Lang.Keyword.For }, BeginToken { Text: Lang.ControlStructureArgBegin },
                KeywordToken { Text: Lang.Keyword.DeclareVar }, IdentifierToken id, OperatorToken { Text: ":" }, ..
            ]) return ts[0].Err<Iteration>();
        ts.RemoveRange(0, 5);

        return Expression.Accept(ts).FlatMap(expr =>
        {
            ts = expr.Remaining.ToList();

            if (ts is not [EndToken { C: ')' }, ..])
                return ts[0].Err<Iteration>();

            ts.RemoveAt(0);
            return Stmt().Accept(ts)
                         .FlatMap(stmt => new Iteration(id.Text, expr.Result, stmt.Result).Ok(stmt.Remaining));
        });
    }
}