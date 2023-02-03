using System.Collections;
using Test.Parser;

namespace Test.Interpreter;

public class Interpreter
{
    private readonly Stack<Dictionary<string, object>> stack;

    public Interpreter()
    {
        var builtIns = BuiltIns.Functions.Enumerate<Function>()
                               .ToDictionary(f => f.Name, f => (object)f);

        stack = new(new[] { builtIns, new() });
    }

    public object Eval(IExpression expr) => expr switch
    {
        Const c => c.Value is string s && double.TryParse(s, out var d) ? d : c.Value,
        Assignment a => ReSet(a.Identifier, Eval(a.Expression)),
        Evaluation i => Resolve<object>(i.Identifier),
        ExpressionLambda l => new Function("λ", l.Parameters, _ => Eval(l.Body)),
        BlockLambda b => new Function("λ", b.Parameters, _ => ExecuteExpectingValue(b.Body)),
        Invocation c => Call(c),
        ListInit l => l.Items.Select(Eval).ToList(),
        BiOperator bo => (bo.Operator, Eval(bo.Left)) switch
        {
            (Lang.Boolean.Or, true) => true,
            (Lang.Boolean.Or, false) => (bool)Eval(bo.Right),
            (Lang.Boolean.And, true) => (bool)Eval(bo.Right),
            (Lang.Boolean.And, false) => false,

            var (op, left) => (op, left, Eval(bo.Right)) switch
            {
                (Lang.Arithmetic.Add, double a, double b) => a + b,
                (Lang.Arithmetic.Sub, double a, double b) => a - b,
                (Lang.Arithmetic.Div, double a, double b) => a / b,
                (Lang.Arithmetic.Mul, double a, double b) => a * b,
                (Lang.Arithmetic.Pow, double a, double b) => Math.Pow(a, b),

                (Lang.Comparison.GT, double a, double b) => a > b,
                (Lang.Comparison.LT, double a, double b) => a < b,
                (Lang.Comparison.GTE, double a, double b) => a >= b,
                (Lang.Comparison.LTE, double a, double b) => a <= b,
                (Lang.Comparison.EQ, double a, double b) => Math.Abs(a - b) / (Math.Abs(a) + Math.Abs(b)) < .5e-9,

                (Lang.Comparison.EQ, var a, var b) => Equals(a, b),

                _ => throw new InvalidOperationException()
            }
        },
        UnaryOperator uo => (uo.Operator, Eval(uo.Arg)) switch
        {
            (Lang.Boolean.Not, bool b) => !b,
            (Lang.Arithmetic.Sub, double x) => -x,
            _ => throw new InvalidOperationException()
        },
        ExpressionBlock eb => ExecuteExpectingValue(eb),
        _ => throw new InvalidOperationException()
    };

    public void Execute(Parser.Program program)
    {
        foreach (var statement in program.Statements)
            Execute(statement);
    }

    public object ExecuteExpectingValue(Block block) => ((IValuedStatus)Execute(block)).Value;

    public Status Execute(Block block)
    {
        stack.Push(new());

        try
        {
            foreach (var s in block.Statements)
            {
                var status = Execute(s);

                if (status is Returning)
                    return status;
            }

            if (block is ExpressionBlock eBlock)
                return new OkWithValue(Eval(eBlock.Return));

            return new Ok();
        }
        finally
        {
            stack.Pop();
        }
    }


    public Status Execute(IStatement statement)
    {
        switch (statement)
        {
            case Return ret:
                return new Returning(Eval(ret.Expression));
            case Initialization init:
                SetNew(init.Identifier, Eval(init.Expression));
                break;
            case Invocation c:
                Call(c);
                break;
            case SingleConditional c:
                if (Eval(c.Condition) is true)
                    return Execute(c.Statement);
                break;
            case DoubleConditional c:
                Execute(Eval(c.Condition) is true ? c.True : c.False);
                break;
            case Block b:
                return Execute(b);
            case Iteration i:
                var enumerable = ((IEnumerable)Eval(i.Enumerable)).Cast<object>();
                var initIterator = true;
                foreach (var item in enumerable)
                {
                    if (initIterator)
                        SetNew(i.Iterator, item);
                    else
                        ReSet(i.Iterator, item);

                    initIterator = false;

                    if (Execute(i.Statement) is Returning r)
                        return r;
                }
                stack.Peek().Remove(i.Iterator);
                break;
            case Loop l:
                while (Eval(l.Condition) is true)
                {
                    if (Execute(l.Body) is Returning r)
                        return r;
                }

                break;
            case IExpression x:
                Eval(x);
                break;
            default:
                throw new InvalidOperationException();
        }

        return new Ok();
    }

    private object Call(Function f, IEnumerable<object> args)
    {
        var a = args.ToArray();

        stack.Push(f.Args.Zip(a).ToDictionary(t => t.First, t => t.Second));
        var ret = f[a];
        stack.Pop();

        return ret;
    }

    private object Call(Invocation c)
    {
        var f = Resolve<Function>(c.Identifier);
        var args = c.Arguments.Select(Eval).ToArray();

        return Call(f, args);
    }

    private void SetNew<T>(string identifier, T value) => stack.Peek().Add(identifier, value!);

    private object ReSet<T>(string identifier, T value)
    {
        foreach (var frame in stack)
        {
            if (frame.ContainsKey(identifier))
                return frame[identifier] = value!;
        }

        throw new InvalidOperationException();
    }

    private T Resolve<T>(string identifier)
    {
        foreach (var frame in stack)
        {
            if (frame.TryGetValue(identifier, out var v))
                return (T)v;
        }

        throw new InvalidOperationException();
    }

    public interface IValuedStatus
    {
        object Value { get; }
    }

    public abstract record Status;

    public record Ok : Status;

    public record OkWithValue(object Value) : Status, IValuedStatus;

    public record Returning(object Value) : Status, IValuedStatus;
}