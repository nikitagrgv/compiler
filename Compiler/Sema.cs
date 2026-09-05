using System.Diagnostics;

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
        Scope scope = new();
        unit.Scope = scope;
        PushScope(scope);

        RegisterBuiltin(scope);

        CollectFunctionsSymbols(unit);
    }

    private void RegisterBuiltin(Scope scope)
    {
        void Register(string name, Type type)
        {
            TypeSymbol symbol = new()
            {
                Name = name,
                DeclaringScope = scope,
                Type = type,
            };
            bool added = scope.TryDeclare(symbol);
            Debug.Assert(added);
        }

        Register("i32", BuiltinType.I32);
    }

    private void CollectFunctionsSymbols(CompilationUnit unit)
    {
        Scope scope = CurrentScope();
        foreach (FuncDecl fd in unit.FuncDecls)
        {
            ReadOnlySpan<char> name = TokenValue(fd.NameToken);
            // fd.Params
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

    private ReadOnlySpan<char> TokenValue(int tokenIndex)
    {
        return _tokens[tokenIndex].Value(_code);
    }

    private void Error(string message, Node node)
    {
    }
}