namespace Compiler;

public class Sema
{
    private readonly string _code;
    private readonly Diagnostic _diag;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly TypeRegistry _typeRegistry = new();

    public Sema(string code, IReadOnlyList<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _tokens = tokens;
    }

    public void Run(CompilationUnit unit)
    {
    }

    private void Error(string message, Node node)
    {
    }

    public void PrintDebug()
    {
    }
}