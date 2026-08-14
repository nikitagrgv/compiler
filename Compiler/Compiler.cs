namespace Compiler;

public class Compiler
{
    public class Flags
    {
        public bool DebugLexer = false;
        public bool DebugLexerPretty = false;
    }

    private IFileSystem _fs;
    private Flags _flags;

    public Compiler(IFileSystem fs, Flags flags)
    {
        _fs = fs;
        _flags = flags;
    }

    public bool Compile(string file, string? output)
    {
        string fullPathFile = _fs.ResolveToFullPath(file);
        string fullPathOutput = _fs.ResolveToFullPath(output ?? "output.o");
        string code = _fs.ReadAll(fullPathFile);
        return Compile(code);
    }

    private bool Compile(string code)
    {
        Lexer lexer = new();
        Lexer.Result lexerResult = lexer.Run(code);
        if (lexerResult.Errors.Count > 0)
        {
            Console.WriteLine("Lexer errors:");
            foreach (string error in lexerResult.Errors)
            {
                Console.WriteLine(error);
            }

            return false;
        }

        if (_flags.DebugLexer)
        {
            Console.WriteLine("================================");
            PrintTokens(lexerResult.Tokens);
            Console.WriteLine("================================");
        }

        if (_flags.DebugLexerPretty)
        {
            Console.WriteLine("================================");
            PrintTokensPretty(lexerResult.Tokens);
            Console.WriteLine("================================");
        }

        return true;
    }

    private void PrintTokens(List<Token> tokens)
    {
        foreach (Token token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    private void PrintTokensPretty(List<Token> tokens)
    {
        int curLine = 0;
        int curColumn = 1;
        foreach (Token token in tokens)
        {
            while (curLine < token.Line)
            {
                curLine++;
                curColumn = 1;
                Console.WriteLine();
                Console.Write($"{curLine,5}:  ");
            }

            if (curColumn >= token.Column)
            {
                Console.Write(" ");
                curColumn = token.Column;
            }

            while (curColumn < token.Column)
            {
                Console.Write(" ");
                curColumn++;
            }

            string str = token.Type.PrettyName();
            if (token.Value != null)
            {
                str += $"\"{token.Value}\"";
            }

            Console.Write(str);
            curColumn += str.Length;
        }

        Console.WriteLine();
    }
}