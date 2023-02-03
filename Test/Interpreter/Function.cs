namespace Test.Interpreter;

public record Function(string Name, IEnumerable<string> Args, Func<IEnumerable<object>, object> F)
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