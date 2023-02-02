using Test.Parser;

namespace Test;

public class Interpreter
{
    private readonly Stack<Dictionary<string, object>> stack;

    public Interpreter()
    {
        var builtIns = new Dictionary<string, object>
        {
            ["print"] = BuiltInPrint(),
            ["map"] = BuiltInMap()
        };

        stack = new(new[] { builtIns, new() });
    }

    public object Eval(Expression expr) => expr switch
    {
        Const c => c.Value is string s && double.TryParse(s, out var d) ? d : c.Value,
        //Arithmetic a => (double)Eval(a.Left) + (double)Eval(a.Right),
        Evaluation i => Resolve<object>(i.Identifier),
        Lambda l => new Function("λ", l.Parameters, _ => Eval(l.Body)),
        Invocation c => Call(c),
        ListInit l => l.Items.Select(Eval),
        BiOperator bo => (bo.Operator, (double)Eval(bo.Left), (double)Eval(bo.Right)) switch
        {
            ("+", var a, var b) => a + b,
            ("-", var a, var b) => a - b,
            ("/", var a, var b) => a / b,
            ("*", var a, var b) => a * b,
            ("^", var a, var b) => Math.Pow(a, b),
            _ => throw new InvalidOperationException()
        },
        UnaryOperator uo => (uo.Operator, (double)Eval(uo.Arg)) switch
        {
            ("-", var x) => -x,
            _ => throw new InvalidOperationException()
        },
        _ => throw new InvalidOperationException()
    };

    public void Execute(Parser.Program program)
    {
        foreach (var statement in program.Statements)
            Execute(statement);
    }

    public void Execute(Statement statement)
    {
        switch (statement)
        {
            case Initialization init:
                SetNew(init.Identifier, Eval(init.Expression));
                break;
            case Assignment a:
                ReSet(a.Identifier, Eval(a.Expression));
                break;
            case Invocation c:
                Call(c);
                break;
            case Conditional c:
                if (Eval(c.Condition) is true)
                    Execute(c.Statement);
                break;
            case Block b:
                foreach(var s in b.Statements)
                    Execute(s);
                break;
            default:
                throw new InvalidOperationException();
        }
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

    private Function BuiltInPrint()
    {
        return new Function("print",
                            o =>
                            {
                                void Print(object obj)
                                {
                                    if (obj is IEnumerable<object> e)
                                    {
                                        Console.Write(Lang.ListBegin);

                                        var x = true;
                                        foreach (var item in e)
                                        {
                                            if (!x)
                                                Console.Write(Lang.ListDelimit + " ");
                                            x = false;

                                            Print(item);
                                        }

                                        Console.Write(Lang.ListEnd);
                                    }
                                    else
                                        Console.Write(obj);
                                }

                                Print(o.Single());
                                Console.WriteLine();

                                return 0;
                            },
                            "arg");
    }

    private Function BuiltInMap()
    {
        return new Function("map",
                            args =>
                            {
                                if (args.ToArray() is not [IEnumerable<object> list, Function f])
                                    throw new ArgumentException();

                                return list.Select(v => Call(f, new[] { v }));
                            },
                            "list",
                            "f");
    }

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