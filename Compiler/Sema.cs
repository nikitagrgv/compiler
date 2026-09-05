namespace Compiler;

public class Sema
{
    private readonly string _code;
    private readonly Diagnostic _diag;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly TypeRegistry _typeRegistry = new();
    private readonly List<Scope> _scopes = new();

    public Sema(string code, IReadOnlyList<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _tokens = tokens;
    }

    public void Run(CompilationUnit unit)
    {
        unit.Scope = new Scope();
        PushScope(unit.Scope);

        CollectFunctionsSymbols(unit);
    }

    private void CollectFunctionsSymbols(CompilationUnit unit)
    {
        foreach (FuncDecl fd in unit.FuncDecls)
        {
        }
    }

    private void PushScope(Scope scope)
    {
        _scopes.Add(scope);
    }

    private void PopScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
    }

    private Scope CurrentScope()
    {
        return _scopes[^1];
    }

    private void Error(string message, Node node)
    {
    }
}