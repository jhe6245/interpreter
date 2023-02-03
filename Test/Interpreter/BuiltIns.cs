using System.Collections;
using System.Runtime.CompilerServices;

namespace Test.Interpreter;

public static class BuiltIns
{
    public static IEnumerable<T> Enumerate<T>(this ITuple t) => Enumerable.Range(0, t.Length).Select(i => (T)t[i]!);

    public static (Function Print, Function PrintLine, Function Format, Function Len, Function Get, Function Set, Function Push, Function Repeat) Functions => (
        new("print",
            args =>
            {
                Console.Write((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg"),
        new("println",
            args =>
            {
                Console.WriteLine((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg"),
        new("format",
            args =>
            {
                var a = args.ToArray();
                return string.Format((string)a[0], a.Skip(1).Select(o => (object)o.Pretty()).ToArray());
            },
            "arg"),
        new("len",
            args => (double)args.Cast<IEnumerable>().Single().Cast<object>().Count(),
            "list"),
        new ("get",
            args =>
            {
                if (args.ToArray() is [IEnumerable list, double idx])
                    return list.Cast<object>().ElementAt((int)idx);

                throw new ArgumentException();
            },
            "list",
            "idx"),
        new("set",
            args =>
            {
                if (args.ToArray() is [IList list, double idx, var v])
                    return list[(int)idx] = v;
                throw new ArgumentException();
            },
            "list",
            "idx",
            "value"),
        new("push",
            args => args.ToArray() switch
            {
                [IList list, var value] => list.Add(value),
                _ => throw new ArgumentException()
            },
            "list",
            "value"),
        new("repeat",
            args => args.ToArray() switch
            {
                [double count] => Enumerable.Repeat<object>(null!, (int)count).ToList(),
                [double count, { } value] => Enumerable.Repeat(value, (int)count).ToList(),
                _ => throw new ArgumentException()
            },
            "count",
            "value")
        );
}