namespace Compiler;

public class Sema
{
    private string _code;
    private Diagnostic _diag;
    private List<Token> _tokens;
    private TypeRegistry _typeRegistry = new();

    public Sema(string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _tokens = tokens;
    }

    public void Run(CompilationUnit unit)
    {
    }

    public void PrintDebug()
    {
    }
}