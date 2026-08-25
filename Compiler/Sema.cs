namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;

    public bool Run(CompilationUnit unit, string code, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        Run(unit);
        return _hasErrors;
    }

    private void Run(CompilationUnit unit)
    {
    }
}