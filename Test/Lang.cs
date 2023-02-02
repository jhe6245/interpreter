namespace Test;

public static class Lang
{
    public static class Operator
    {
        public const string Add = "+";
    }

    public static class Keyword
    {
        public const string DeclareVar = "let";
        public const string True = "true";
        public const string False = "false";
        
        public const string If = "if";
        public const string For = "for";

        public static bool Contains(string str) => new[]
        {
            DeclareVar,
            True, False,
            If, For
        }.Contains(str);
    }

    public const string StatementDelimiter = ";";

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