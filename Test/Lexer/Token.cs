﻿namespace Test.Lexer;

public record Token(string Text);

public record StringToken(string Text) : Token(Text);

public record NumericToken(string Text) : Token(Text);

public record IdentifierToken(string Text) : Token(Text);

public record BeginToken(char C) : Token(C.ToString());

public record EndToken(char C) : Token(C.ToString());

public abstract record MathToken(string Op) : Token(Op);

public record ArithmeticToken(string Op) : MathToken(Op);

public record BooleanToken(string Op) : MathToken(Op);

public record ArrowToken(string Text) : Token(Text);

public record KeywordToken(string Text) : Token(Text);

public record OperatorToken(string Text) : Token(Text);

public record EndStatementToken(string Text) : Token(Text);

public record ListSepToken(string Text) : Token(Text);
