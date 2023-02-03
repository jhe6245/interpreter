using System.Collections;

namespace Test.Interpreter;

public static class BuiltIns
{
    public static Function Print =>
        new("print",
            args =>
            {
                Console.Write((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg");

    public static Function PrintLine =>
        new("println",
            args =>
            {
                Console.WriteLine((args.SingleOrDefault() ?? args).Pretty());
                return 0;
            },
            "arg");

    public static Function Format =>
        new("format",
            args =>
            {
                var a = args.ToArray();
                return string.Format((string)a[0], a.Skip(1).Select(o => (object)o.Pretty()).ToArray());
            },
            "arg");

    public static Function Len =>
        new("len",
            args => (double)args.Cast<IEnumerable>().Single().Cast<object>().Count(),
            "list");

    public static Function Get
        => new("get",
               args =>
               {
                   if (args.ToArray() is [IEnumerable list, double idx])
                       return list.Cast<object>().ElementAt((int)idx);

                   throw new ArgumentException();
               },
               "list",
               "idx");

    public static Function Set
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

    public static Function Repeat =>
        new("repeat",
            args => args.ToArray() switch
            {
                [double count] => Enumerable.Repeat<object>(null!, (int)count).ToList(),
                [double count, { } value] => Enumerable.Repeat(value, (int)count).ToList(),
                _ => throw new ArgumentException()
            },
            "count",
            "value");
}