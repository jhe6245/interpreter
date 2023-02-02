namespace Test.Lexer;

public record Token(string Text);

public record StringToken(string Text) : Token(Text);

public record NumericToken(string Text) : Token(Text);

public record IdentifierToken(string Text) : Token(Text);

public record BeginToken(char C) : Token(C.ToString());

public record EndToken(char C) : Token(C.ToString());

public abstract record OperatorToken(string Op) : Token(Op);

public record ArithmeticToken(string Op) : OperatorToken(Op);
public record BooleanToken(string Op) : OperatorToken(Op);
public record ComparisonToken(string Text) : OperatorToken(Text);


public record ArrowToken(string Text) : Token(Text);

public record KeywordToken(string Text) : Token(Text);

public record LangOperatorToken(string Text) : Token(Text);

public record EndStatementToken(string Text) : Token(Text);

public record ListSepToken(string Text) : Token(Text);

