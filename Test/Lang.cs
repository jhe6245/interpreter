namespace Test;

public static class Lang
{
    public static class Comparison
    {
        public const string GT = ">";
        public const string LT = "<";
        public const string GTE = ">=";
        public const string LTE = "<=";
        public const string EQ = "=";

        public static bool Contains(string str) => new[]
        {
            GT, LT, GTE, LTE, EQ
        }.Contains(str);
    }

    public static class Arithmetic
    {
        public const string Pow = "^";
        public const string Mul = "*";
        public const string Div = "/";
        public const string Add = "+";
        public const string Sub = "-";

        public static bool Contains(string str) => new[]
        {
            Pow, Mul, Div, Add, Sub
        }.Contains(str);
    }

    public static class Boolean
    {
        public const string Not = "not";
        public const string And = "and";
        public const string Or = "or";

        public static bool Contains(string str) => new[]
        {
            Not, And, Or
        }.Contains(str);
    }

    public static class Keyword
    {
        public const string DeclareVar = "let";
        public const string True = "true";
        public const string False = "false";
        
        public const string If = "if";
        public const string Else = "else";
        public const string For = "for";
        public const string Return = "return";

        public static bool Contains(string str) => new[]
        {
            DeclareVar,
            True, False,
            If, Else, For, Return
        }.Contains(str);
    }

    public const string Assign = ":=";
    
    public const char StringDelimiter = '\'';

    public const string ExprBegin = "(";
    public const string ExprEnd = ")";

    public const string ArgListBegin = "(";
    public const string ArgListEnd = ")";

    public const string LambdaArrow = "->";
    public const string ArgListDelimit = ",";

    public const string ListBegin = "[";
    public const string ListDelimit = ",";
    public const string ListEnd = "]";

    public const string BlockBegin = "{";
    public const string BlockEnd = "}";

    public const string ControlStructureArgBegin = "(";
    public const string ControlStructureArgEnd = ")";
}