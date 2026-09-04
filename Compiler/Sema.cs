namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];
    private TypeRegistry _typeRegistry = new();

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;
        _typeRegistry = new TypeRegistry();

        Run(unit);

        return !_hasErrors;
    }

    private void Run(CompilationUnit unit)
    {
    }

    public void PrintDebug()
    {
    }
}