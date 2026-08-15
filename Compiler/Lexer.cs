namespace Compiler;

public class Lexer
{
    private readonly string _code;

    public struct Result
    {
        public List<Token> Tokens;
        public List<string> Errors;
    }

    public Lexer(string code)
    {
        _code = code;
    }

    public Result Run()
    {
        List<Token> tokens = [];
        List<string> errors = [];
        int len = _code.Length;
        int pos = 0;
        int line = 1;
        int column = 1;
        bool comment = false;

        while (pos < len)
        {
            char c = _code[pos];

            if (c == '/' && pos + 1 < len && _code[pos + 1] == '/')
            {
                comment = true;
            }

            switch (c)
            {
                case '\n':
                    comment = false;
                    line++;
                    column = 1;
                    pos++;
                    continue;
                case ' ':
                    pos++;
                    column++;
                    continue;
            }

            if (comment)
            {
                pos++;
                column++;
                continue;
            }

            if (c == '\t')
            {
                errors.Add($"Unexpected tab character ({line}:{column})");
                break;
            }

            if (char.IsWhiteSpace(c))
            {
                pos++;
                column++;
                continue;
            }

            if (TryParseSingleCharToken(c) is { } singleCharToken)
            {
                Token token = new()
                {
                    Type = singleCharToken,
                    Position = pos,
                    Length = 1,
                    Line = line,
                    Column = column,
                };
                tokens.Add(token);
                pos++;
                column++;
                continue;
            }

            if (TryParseLiteralInt(_code.AsSpan(pos), out int valueLen, out bool valid))
            {
                Token token = new()
                {
                    Type = TokenType.LiteralInt,
                    Position = pos,
                    Length = valueLen,
                    Line = line,
                    Column = column,
                };

                if (!valid)
                {
                    errors.Add($"Invalid integer literal ({line}:{column}): {token.Value(_code)}");
                    break;
                }

                tokens.Add(token);
                pos += valueLen;
                column += valueLen;
                continue;
            }

            ReadOnlySpan<char> word = ParseWord(_code.AsSpan(pos));
            if (word.IsEmpty)
            {
                errors.Add($"Unexpected token ({line}:{column})");
                break;
            }

            if (TryParseKeyword(word) is { } keywordType)
            {
                Token token = new()
                {
                    Type = keywordType,
                    Position = pos,
                    Length = word.Length,
                    Line = line,
                    Column = column,
                };
                tokens.Add(token);
            }
            else
            {
                Token token = new()
                {
                    Type = TokenType.Identifier,
                    Position = pos,
                    Length = word.Length,
                    Line = line,
                    Column = column,
                };
                tokens.Add(token);
            }

            pos += word.Length;
            column += word.Length;
        }

        tokens.Add(new Token()
        {
            Type = TokenType.Eof,
            Position = pos,
            Length = 0,
            Line = line,
            Column = column
        });

        Result result = new()
        {
            Tokens = tokens,
            Errors = errors
        };

        return result;
    }

    private TokenType? TryParseKeyword(ReadOnlySpan<char> word)
    {
        return word switch
        {
            "fn" => TokenType.KeywordFunc,
            "return" => TokenType.KeywordReturn,
            "let" => TokenType.KeywordLet,
            _ => null
        };
    }

    private static ReadOnlySpan<char> ParseWord(ReadOnlySpan<char> str)
    {
        if (!IsWordStart(str[0]))
        {
            return "";
        }

        int pos = 1;
        while (pos < str.Length && IsWordPart(str[pos]))
        {
            pos++;
        }

        ReadOnlySpan<char> word = str[..pos];
        return word;
    }

    private static bool TryParseLiteralInt(ReadOnlySpan<char> str, out int len, out bool valid)
    {
        len = 0;
        valid = true;

        if (!char.IsAsciiDigit(str[0]))
        {
            return false;
        }

        int pos = 0;
        bool hexOrBinary = false;
        if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hexOrBinary = true;
            pos = 2;
            while (pos < str.Length && char.IsAsciiHexDigit(str[pos]))
            {
                pos++;
            }
        }
        else if (str.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            hexOrBinary = true;
            pos = 2;
            while (pos < str.Length && (str[pos] == '0' || str[pos] == '1'))
            {
                pos++;
            }
        }
        else if (str[0] == '0')
        {
            pos = 1;
            while (pos < str.Length && IsOctalDigit(str[pos]))
            {
                pos++;
            }
        }
        else
        {
            while (pos < str.Length && char.IsAsciiDigit(str[pos]))
            {
                pos++;
            }
        }

        while (pos < str.Length && (char.IsAsciiLetterOrDigit(str[pos]) || str[pos] == '_'))
        {
            valid = false;
            pos++;
        }

        len = pos;

        if (hexOrBinary && len <= 2)
        {
            valid = false;
        }

        return true;
    }

    private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';

    private static bool IsWordStart(char c) => char.IsAsciiLetter(c) || c == '_';
    private static bool IsWordPart(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    private static TokenType? TryParseSingleCharToken(char c)
    {
        return c switch
        {
            '(' => TokenType.LPar,
            ')' => TokenType.RPar,
            '{' => TokenType.LBrace,
            '}' => TokenType.RBrace,
            ':' => TokenType.Colon,
            ';' => TokenType.Semicolon,
            '=' => TokenType.Assign,
            '+' => TokenType.Plus,
            '-' => TokenType.Minus,
            '*' => TokenType.Star,
            '/' => TokenType.Slash,
            '%' => TokenType.Percent,
            ',' => TokenType.Comma,
            _ => null
        };
    }
}