using Test.Lexer;

namespace Test.Parser;

public class ExpressionParser : IParse<Expression> // math + bool logic
{
    public required IParse<Expression> Value { get; init; }

    public IResult<IParsed<Expression>> Accept(IEnumerable<Token> tokens) =>
        ParseE(tokens).FlatMap(e => ShuntingYard(e.Result).FlatMap(expr => expr.Ok(e.Remaining)));

    private static IResult<Expression> ShuntingYard(E expression)
    {
        var operators = new Stack<Operator>();
        var operands = new Stack<Expression>();

        void Collect()
        {
            var o = operators.Pop();
            var latter = operands.Pop();
            if (o is BinaryOp)
                operands.Push(new BiOperator(o.Op, operands.Pop(), latter));
            else
                operands.Push(new UnaryOperator(o.Op, latter));
        }

        void HandleOp(Operator op)
        {
            while (operators.Count > 0 && operators.Peek().CompareTo(op) > 0)
                Collect();
            operators.Push(op);
        }

        void HandleE(E e)
        {
            HandleP(e.Head);
            foreach (var (op, p) in e.Tail)
            {
                HandleOp(new BinaryOp(op.Op));
                HandleP(p);
            }

            while (operands.Count > 1)
                Collect();
        }

        void HandleP(P p)
        {
            switch (p)
            {
                case Val { V: var v }:
                    operands.Push(v);
                    break;

                case Parens { E: var e }:
                    if (ShuntingYard(e) is IOk<Expression> ok)
                        operands.Push(ok.Result);
                    break;

                case Unary { Op: var c, P: var p1 }:
                    HandleOp(new UnaryOp(c));
                    HandleP(p1);
                    break;
            }
        }

        HandleE(expression);
        return Result.Ok(operands.Single());
    }

    private IResult<IParsed<E>> ParseE(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        var headR = ParseP(ts);

        if (headR is not IOk<IParsed<P>> { Result: var head })
            return (headR as IErr<IParsed<P>>)!.To<IParsed<E>>();

        ts = head.Remaining.ToArray();

        var tail = new List<(Binary, P)>();

        while (ts.Length > 0)
        {
            var bp = (ts switch
            {
                [MathToken { Text: var op }, ..] => new Binary(op).Ok(ts.Skip(1)),
                _ => ts[0].Err<Binary>()
            }).FlatMap(b => ParseP(b.Remaining).FlatMap(p => (b.Result, p.Result).Ok(p.Remaining)));

            if (bp is IOk<IParsed<(Binary, P)>> { Result: var parsedBp })
            {
                tail.Add(parsedBp.Result);
                ts = parsedBp.Remaining.ToArray();
            }
            else
                break;
        }

        return new E(head.Result, tail).Ok(ts);
    }

    private IResult<IParsed<P>> ParseP(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts switch
        {
            _ when Value.Accept(ts) is IOk<IParsed<Expression>> ok =>
                ok.FlatMap(e => new Val(e.Result).Ok(e.Remaining)),

            [BeginToken { C: '(' }, ..] => ParseE(ts.Skip(1)).FlatMap(e => e.Remaining.ToArray() switch
            {
                [EndToken { C: ')' }, ..] => new Parens(e.Result).Ok(e.Remaining.Skip(1)),
                _ => e.Remaining.First().Err<Parens>()
            }),

            [MathToken { Op: Lang.Boolean.Not or Lang.Arithmetic.Sub } t, ..] => ParseP(ts.Skip(1))
                .FlatMap(e => new Unary(t.Op, e.Result).Ok(e.Remaining)),
            _ => ts[0].Err<P>()
        };
    }

    private abstract record Operator(string Op) : IComparable<Operator>
    {
        public abstract int CompareTo(Operator? other);

        protected static bool RightAssoc(string Op) => Op is Lang.Arithmetic.Pow or Lang.Boolean.Not;

        protected static int Precedence(string op) =>
            op switch
            {
                Lang.Boolean.Or => -3,
                Lang.Boolean.And => -2,
                Lang.Boolean.Not => -1,

                Lang.Arithmetic.Add or Lang.Arithmetic.Sub => 1,
                Lang.Arithmetic.Mul or Lang.Arithmetic.Div => 2,
                Lang.Arithmetic.Pow => 3,
                _ => throw new ArgumentException(null, nameof(op))
            };
    }

    private record UnaryOp(string Op) : Operator(Op)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
                return Precedence(Op) >= Precedence(other.Op) ? 1 : 0;

            return Precedence(Op) > Precedence(other!.Op) ? 1 : 0;
        }
    }

    private record BinaryOp(string Op) : Operator(Op)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
            {
                return Precedence(Op).CompareTo(Precedence(other.Op)) switch
                {
                    > 0 => 1,
                    0 when !RightAssoc(Op) => 1,
                    _ => -1
                };
            }

            return 0;
        }
    }

    private record E(P Head, IEnumerable<(Binary, P)> Tail);

    private abstract record P;

    private record Val(Expression V) : P;

    private record Parens(E E) : P;

    private record Unary(string Op, P P) : P;

    private record Binary(string Op);
}