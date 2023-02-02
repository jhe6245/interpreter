namespace Test.Lexer;

public class Lexer
{
    public IEnumerable<Token> Lex(string program)
    {
        var reader = new StringReader(program);

        while (reader.TryPeek(out var character))
        {
            switch (character)
            {
                case Lang.StringDelimiter:
                    reader.Read();
                    var str = reader.ReadWhile(c => c != Lang.StringDelimiter);
                    yield return new StringToken(str);
                    reader.Read();
                    break;

                case { } when char.IsNumber(character) || character == '.':
                    var n = reader.ReadWhile(c => char.IsNumber(c) || c is '.');
                    yield return new NumericToken(n);
                    break;

                case { } when char.IsLetterOrDigit(character) || character == '_':
                    var word = reader.ReadWhile(c => char.IsLetterOrDigit(c) || c == '_');
                    if (Lang.Keyword.Contains(word))
                        yield return new KeywordToken(word);
                    else
                        yield return new IdentifierToken(word);
                    break;

                case ':':
                    reader.Read();
                    if (reader.TryPeek(out var next) && next == '=')
                    {
                        reader.Read();
                        yield return new OperatorToken(":=");
                        break;
                    }

                    yield return new OperatorToken(":");
                    break;

                case { } when "(<[{".Contains(character):
                    reader.Read();
                    yield return new BeginToken(character);
                    break;

                case { } when ")>]}".Contains(character):
                    reader.Read();
                    yield return new EndToken(character);
                    break;

                case { } when "+-*/^".Contains(character):
                    reader.Read();
                    if (character == '-')
                    {
                        if ((char)reader.Peek() == '>')
                        {
                            reader.Read();
                            yield return new ArrowToken("->");
                            break;
                        }
                    } 
                    yield return new ArithmeticToken(character);
                    break;

                case ';':
                    reader.Read();
                    yield return new EndStatementToken(";");
                    break;

                case ',':
                    reader.Read();
                    yield return new ListSepToken(",");
                    break;

                default:
                    reader.Read();
                    break;

            }
        }
    }
}