using Test.Lexer;

namespace Test.Parser.Constructs;

public class ExpressionParser : IParse<IExpression> // math + boolean logic + comparisons
{
    public required IParse<IExpression> ValueExpression { get; init; }

    public IResult<IParsed<IExpression>> Accept(IEnumerable<Token> tokens) =>
        ParseMath(tokens).FlatMap(e => ShuntingYard(e.Result).FlatMap(expr => expr.Ok(e.Remaining)));

    private static IResult<IExpression> ShuntingYard(MathExpr expr)
    {
        var operators = new Stack<Operator>();
        var operands = new Stack<IExpression>();

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

        void HandleExpr(MathExpr e)
        {
            HandleOperand(e.Head);
            foreach (var (op, p) in e.Tail)
            {
                HandleOp(new BinaryOp(op.Op));
                HandleOperand(p);
            }
            while (operators.Count > 0)
                Collect();
        }

        void HandleOperand(BiOperand p)
        {
            switch (p)
            {
                case Val { V: var v }:
                    operands.Push(v);
                    break;

                case Parens { MathExpr: var e }:
                    if (ShuntingYard(e) is IOk<IExpression> ok)
                        operands.Push(ok.Result);
                    break;

                case Unary { Op: var c, BiOperand: var p1 }:
                    HandleOp(new UnaryOp(c));
                    HandleOperand(p1);
                    break;
            }
        }

        HandleExpr(expr);
        return Result.Ok(operands.Single());
    }

    private IResult<IParsed<MathExpr>> ParseMath(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        var headR = ParseBinaryOperand(ts);

        if (headR is not IOk<IParsed<BiOperand>> { Result: var head })
            return (headR as IErr<IParsed<BiOperand>>)!.To<IParsed<MathExpr>>();

        ts = head.Remaining.ToArray();

        var tail = new List<(Binary, BiOperand)>();

        while (ts.Length > 0)
        {
            var bp = (ts switch
            {
                [OperatorToken { Text: var op }, ..] => new Binary(op).Ok(ts.Skip(1)),
                _ => ts[0].Err<Binary>()
            }).FlatMap(b => ParseBinaryOperand(b.Remaining).FlatMap(p => (b.Result, p.Result).Ok(p.Remaining)));

            if (bp is IOk<IParsed<(Binary, BiOperand)>> { Result: var parsedBp })
            {
                tail.Add(parsedBp.Result);
                ts = parsedBp.Remaining.ToArray();
            }
            else
                break;
        }

        return new MathExpr(head.Result, tail).Ok(ts);
    }

    private IResult<IParsed<BiOperand>> ParseBinaryOperand(IEnumerable<Token> tokens)
    {
        var ts = tokens.ToArray();

        return ts switch
        {
            _ when ValueExpression.Accept(ts) is IOk<IParsed<IExpression>> ok =>
                ok.FlatMap(e => new Val(e.Result).Ok(e.Remaining)),

            [BeginToken { C: '(' }, ..] => ParseMath(ts.Skip(1)).FlatMap(e => e.Remaining.ToArray() switch
            {
                [EndToken { C: ')' }, ..] => new Parens(e.Result).Ok(e.Remaining.Skip(1)),
                _ => e.Remaining.First().Err<Parens>()
            }),

            [OperatorToken { Op: Lang.Boolean.Not or Lang.Arithmetic.Sub } t, ..] => ParseBinaryOperand(ts.Skip(1))
                .FlatMap(e => new Unary(t.Op, e.Result).Ok(e.Remaining)),

            _ => ts[0].Err<BiOperand>()
        };
    }

    private abstract record Operator(string Op) : IComparable<Operator>
    {
        protected bool RightAssoc => Op is Lang.Arithmetic.Pow or Lang.Boolean.Not;

        public int Precedence => Op switch
        {
            Lang.Boolean.Or => -5,
            Lang.Boolean.And => -4,
            Lang.Boolean.Not => -3,

            Lang.Comparison.EQ => -2,
            Lang.Comparison.LT or Lang.Comparison.GT or Lang.Comparison.LTE or Lang.Comparison.GTE => -1,

            Lang.Arithmetic.Add or Lang.Arithmetic.Sub => 1,
            Lang.Arithmetic.Mul or Lang.Arithmetic.Div => 2,
            Lang.Arithmetic.Pow => 3,
            _ => throw new InvalidOperationException()
        };

        public abstract int CompareTo(Operator? other);
    }

    private record UnaryOp(string Op) : Operator(Op)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
                return Precedence >= other.Precedence ? 1 : 0;

            return Precedence > other!.Precedence ? 1 : 0;
        }
    }

    private record BinaryOp(string Op) : Operator(Op)
    {
        public override int CompareTo(Operator? other)
        {
            if (other is BinaryOp)
            {
                return Precedence.CompareTo(other.Precedence) switch
                {
                    > 0 => 1,
                    0 when !RightAssoc => 1,
                    _ => -1
                };
            }

            return 0;
        }
    }

    private record MathExpr(BiOperand Head, IEnumerable<(Binary, BiOperand)> Tail);

    private abstract record BiOperand;

    private record Val(IExpression V) : BiOperand;

    private record Parens(MathExpr MathExpr) : BiOperand;

    private record Unary(string Op, BiOperand BiOperand) : BiOperand;

    private record Binary(string Op);
}