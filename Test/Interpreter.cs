using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Test.Parser;

namespace Test;

public class Interpreter
{
    private readonly Stack<Dictionary<string, object>> stack;

    public Interpreter()
    {
        var builtIns = new[]
        {
            BuiltInLen(), BuiltInMap(), BuiltInPrint(), BuiltInPrintLine(), BuiltInFormat(), BuiltInSet(), BuiltInGet(),
            BuiltInRepeat()
        }.ToDictionary(f => f.Name, f => (object)f);

        stack = new(new[] { builtIns, new() });
    }

    public object Eval(IExpression expr) => expr switch
    {
        Const c => c.Value is string s && double.TryParse(s, out var d) ? d : c.Value,
        //Arithmetic a => (double)Eval(a.Left) + (double)Eval(a.Right),
        Evaluation i => Resolve<object>(i.Identifier),
        ExpressionLambda l => new Function("λ", l.Parameters, _ => Eval(l.Body)),
        BlockLambda b => new Function("λ", b.Parameters, _ => ExecEvalRequired(b.Body)),
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
        ExpressionBlock eb => ExecEvalRequired(eb),
        _ => throw new InvalidOperationException()
    };

    public void Execute(Parser.Program program)
    {
        foreach (var statement in program.Statements)
            Execute(statement);
    }

    public object ExecEvalRequired(Block block) =>
        ExecEval(block, out var ret) ? ret : throw new InvalidOperationException("no return value");

    public bool ExecEval(Block block, [NotNullWhen(true)] out object? value)
    {
        stack.Push(new());
        foreach (var s in block.Statements)
        {
            if (Execute(s) is not Returning { Value: var v })
                continue;

            stack.Pop();
            value = v;
            return true;
        }

        if (block is ExpressionBlock eBlock)
        {
            value = Eval(eBlock.Return);
            return true;
        }

        value = null;
        return false;
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
            case Assignment a:
                ReSet(a.Identifier, Eval(a.Expression));
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
                foreach (var s in b.Statements)
                {
                    if (Execute(s) is Returning r)
                        return r;
                }

                break;
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

                break;
            default:
                throw new InvalidOperationException();
        }

        return new Next();
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

    private void ReSet<T>(string identifier, T value)
    {
        foreach (var frame in stack)
        {
            if (frame.ContainsKey(identifier))
            {
                frame[identifier] = value!;
                return;
            }
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

    private static Function BuiltInPrint() =>
        new("print",
            args =>
            {
                Console.Write((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg");

    private static Function BuiltInPrintLine() =>
        new("println",
            args =>
            {
                Console.WriteLine((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg");

    private static Function BuiltInFormat() =>
        new("format",
            args =>
            {
                var a = args.ToArray();
                return string.Format((string)a[0], a.Skip(1).Select(o => (object)o.Pretty()).ToArray());
            },
            "arg");

    private Function BuiltInMap() =>
        new("map",
            args =>
            {
                if (args.ToArray() is not [IEnumerable<object> list, Function f])
                    throw new ArgumentException();

                return list.Select(v => Call(f, new[] { v }));
            },
            "list",
            "f");

    private Function BuiltInLen() =>
        new("len",
            args => args.Cast<IEnumerable>().Single().Cast<object>().Count(),
            "list");

    private Function BuiltInGet()
        => new("get",
               args =>
               {
                   if (args.ToArray() is [IEnumerable list, double idx])
                       return list.Cast<object>().ElementAt((int)idx);

                   throw new ArgumentException();
               },
               "list",
               "idx");

    private Function BuiltInSet()
        => new("set",
               args =>
               {
                   if (args.ToArray() is [IList list, double idx, var v])
                       return list[(int)idx] = v; 
                   throw new ArgumentException();
               },
               "list",
               "idx",
               "value");

    private Function BuiltInRepeat() =>
        new("repeat",
            args =>
            {
                if (args.ToArray() is [{ } value, double count])
                    return Enumerable.Repeat(value, (int)count);
                throw new ArgumentException();
            },
            "value", "count");

    public abstract record Status;

    public record Next : Status;

    public record Returning(object Value) : Status;

    private record Function(string Name, IEnumerable<string> Args, Func<IEnumerable<object>, object> F)
    {
        public Function(string identifier, Func<IEnumerable<object>, object> f, params string[] args) : this(
            identifier,
            args,
            f)
        {
        }

        public object this[IEnumerable<object> args] => F(args);

        public static implicit operator Func<IEnumerable<object>, object>(Function f) => f.F;

        public override string ToString() =>
            $"{Name}{Lang.ArgListBegin}{string.Join(Lang.ArgListDelimit + " ", Args)}{Lang.ArgListEnd}";
    }
}