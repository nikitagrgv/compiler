namespace Compiler;

public enum TokenType
{
    LPar,
    RPar,
    LBrace,
    RBrace,
    Comma,
    Colon,
    Semicolon,

    Assign,
    Plus,
    Minus,
    Star,
    Slash,

    KeywordFunc,
    KeywordReturn,
    KeywordLet,

    Identifier,

    LiteralInt,

    Eof,
}

public static class TokenUtils
{
    extension(TokenType type)
    {
        public string PrettyName()
        {
            return type switch
            {
                TokenType.LPar => "(",
                TokenType.RPar => ")",
                TokenType.LBrace => "{",
                TokenType.RBrace => "}",
                TokenType.Comma => ",",
                TokenType.Colon => ":",
                TokenType.Semicolon => ";",

                TokenType.Assign => "=",
                TokenType.Plus => "+",
                TokenType.Minus => "-",
                TokenType.Star => "*",
                TokenType.Slash => "/",

                TokenType.KeywordFunc => "fn",
                TokenType.KeywordReturn => "return",
                TokenType.KeywordLet => "let",

                TokenType.Identifier => "$",

                TokenType.LiteralInt => "i",

                TokenType.Eof => "EOF",

                _ => throw new Exception($"Unknown token type: {type}")
            };
        }

        public bool IsLiteral
        {
            get
            {
                return type switch
                {
                    TokenType.LiteralInt => true,
                    _ => false
                };
            }
        }
    }
}

public struct Token
{
    public TokenType Type;
    public string Value;
    public int Line;
    public int Column;

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Value))
        {
            return $"{Line}:{Column} {Type}";
        }

        return $"{Line}:{Column} {Type} \"{Value}\"";
    }
}

public class Lexer
{
    public struct Result
    {
        public List<Token> Tokens;
        public List<string> Errors;
    }

    public Result Run(string code)
    {
        List<Token> tokens = [];
        List<string> errors = [];
        int len = code.Length;
        int pos = 0;
        int line = 1;
        int column = 1;
        bool comment = false;

        while (pos < len)
        {
            char c = code[pos];

            if (c == '/' && pos + 1 < len && code[pos + 1] == '/')
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
                    Line = line,
                    Column = column,
                };
                tokens.Add(token);
                pos++;
                column++;
                continue;
            }

            if (TryParseLiteralInt(code.AsSpan(pos), out string value, out int valueLen, out bool valid))
            {
                if (!valid)
                {
                    errors.Add($"Invalid integer literal ({line}:{column}): {value}");
                    break;
                }

                Token token = new()
                {
                    Type = TokenType.LiteralInt,
                    Value = value,
                    Line = line,
                    Column = column,
                };
                tokens.Add(token);
                pos += valueLen;
                column += valueLen;
                continue;
            }

            string word = ParseWord(code.AsSpan(pos));
            if (string.IsNullOrEmpty(word))
            {
                errors.Add($"Unexpected token ({line}:{column})");
                break;
            }

            if (TryParseKeyword(word) is { } keywordType)
            {
                Token token = new()
                {
                    Type = keywordType,
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
                    Value = word,
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

    private TokenType? TryParseKeyword(string word)
    {
        return word switch
        {
            "fn" => TokenType.KeywordFunc,
            "return" => TokenType.KeywordReturn,
            "let" => TokenType.KeywordLet,
            _ => null
        };
    }

    private static string ParseWord(ReadOnlySpan<char> str)
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

        return str[..pos].ToString();
    }

    private static bool TryParseLiteralInt(ReadOnlySpan<char> str, out string value, out int len, out bool valid)
    {
        value = "";
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
        value = str[..len].ToString();

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
            ',' => TokenType.Comma,
            _ => null
        };
    }
}