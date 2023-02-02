using System.Text;

namespace Test;

public static class CommonExt
{
    public static bool TryPeek(this StringReader reader, out char c)
    {
        var i = reader.Peek();
        c = i != -1 ? (char)i : '\0';
        return i != -1;
    }


    public static bool TryRead(this StringReader reader, out char c)
    {
        var i = reader.Read();
        c = i != -1 ? (char)i : '\0';
        return i != -1;
    }

    public static string ReadWhile(this StringReader reader, Predicate<char> f)
    {
        var b = new StringBuilder();

        while (reader.TryPeek(out var c) && f(c))
        {
            b.Append(c);
            reader.Read();
        }

        return b.ToString();
    }

    public static string Pretty(this object? o, int maxDepth = 10) => o switch
    {
        _ when maxDepth == 0 => "\\...\\",
        null => "\\null\\",
        string s => s,
        Type t => t.ToString(),
        System.Collections.IEnumerable e => $"[ {string.Join(", ", e.Cast<object>().Select(v => v.Pretty(maxDepth - 1)))} ]",

        _ => o.GetType() switch
        {
            { IsPrimitive: true } or { IsEnum: true }  => o.ToString() ?? "",
            var t => $"{t.Name} {{ {string.Join(", ", t.GetProperties()
                                                       .Where(p => p.GetIndexParameters().Length == 0)
                                                       .Select(p => $"{p.Name} = {p.GetValue(o).Pretty(maxDepth - 1)}"))} }}"
        }
    };
}