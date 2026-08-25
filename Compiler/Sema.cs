namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;

        Run(unit);

        return _hasErrors;
    }

    private void Run(CompilationUnit unit)
    {
    }
}