using System.Runtime.CompilerServices;
using Test.Lexer;

namespace Test.Parser;

public class ExpressionParser : IParse<Expression> // math
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
            var a = operands.Pop();
            if (o is BinaryOp)
                operands.Push(new BiOperator(o.C.ToString(), a, operands.Pop()));
            else
                operands.Push(new UnaryOperator(o.C.ToString(), a));
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

                case Unary { P: var p1 }:
                    HandleOp(new UnaryOp('-'));
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
                [ArithmeticToken { C: var c }, ..] when "+-*/^".Contains(c) =>
                    new Binary(c).Ok(ts.Skip(1)),

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

            [ArithmeticToken { C: var c }, ..] when "^-".Contains(c) => ParseP(ts.Skip(1))
                .FlatMap(e => new Unary(c, e.Result).Ok(e.Remaining)),
            _ => ts[0].Err<P>()
        };
    }

    private abstract record Operator(char C) : IComparable<Operator>
    {
        public abstract int CompareTo(Operator? other);

        protected static int Precedence(char o) => "+-*/^".IndexOf(o) / 2;
    }

    private record UnaryOp(char C) : Operator(C)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
                return Precedence(C) >= Precedence(other.C) ? 1 : 0;

            return Precedence(C) > Precedence(other!.C) ? 1 : 0;
        }
    }

    private record BinaryOp(char B) : Operator(B)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
            {
                return (Precedence(B) > Precedence(other.C)) switch
                {
                    true => 1,
                    _ when B != '^' && Precedence(B) == Precedence(other.C) => 1,
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

    private record Unary(char Op, P P) : P;

    private record Binary(char Op);
}