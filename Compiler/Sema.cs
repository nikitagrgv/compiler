using System.Diagnostics;

namespace Compiler;

public class Sema
{
    private readonly string _code;
    private readonly Diagnostic _diag;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly TypeRegistry _typeRegistry = new();
    private readonly List<Scope> _scopes = new(); // TODO: Do we need list? Or just current scope?

    public Sema(string code, IReadOnlyList<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _tokens = tokens;
    }

    public void Run(CompilationUnit unit)
    {
        Scope scope = new(null);
        unit.Scope = scope;
        PushScope(scope);

        RegisterBuiltin(scope);

        RegisterFunctionSymbols(unit);

        VisitCompilationUnit(unit);
    }

    private void VisitCompilationUnit(CompilationUnit unit)
    {
        foreach (FuncDecl fd in unit.FuncDecls)
        {
            VisitFuncDecl(fd);
        }
    }

    private void VisitFuncDecl(FuncDecl fd)
    {
        Debug.Assert(fd.Symbol != null, $"Must be registered in {nameof(RegisterFunctionSymbols)}");

        Scope scope = new(CurrentScope());
        PushScope(scope);

        foreach (Param param in fd.Params)
        {
            Debug.Assert(param.Type.ResolvedType != null, $"Must be resolved in {nameof(RegisterFunctionSymbols)}");

            Type type = param.Type.ResolvedType;
            ReadOnlySpan<char> name = TokenValue(param.NameToken);

            ParamSymbol sym = new()
            {
                Declaration = param,
                DeclaringScope = scope,
                Type = type,
                Name = name.ToString()
            };

            param.Symbol = sym;
            RegisterSymbol(sym, scope);
        }

        fd.Body.Scope = scope;
        VisitBlock(fd.Body);

        PopScope();
    }

    private void VisitBlock(Block block)
    {
        Debug.Assert(block.Scope != null, "Block scope must be set from outside");
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

        // NOTE: Don't register void because it's not supposed to be used by user
        Register("i32", BuiltinType.I32);
    }

    private void RegisterFunctionSymbols(CompilationUnit unit)
    {
        foreach (FuncDecl fd in unit.FuncDecls)
        {
            AddFunctionSymbol(fd);
        }
    }

    private void AddFunctionSymbol(FuncDecl fd)
    {
        Type returnType = BuiltinType.Void;
        if (fd.ReturnType != null)
        {
            ResolveType(fd.ReturnType);
            returnType = fd.ReturnType.ResolvedType!;
        }

        // TODO: Reuse list
        List<Type> paramTypes = [];
        foreach (Param param in fd.Params)
        {
            ResolveType(param.Type);
            paramTypes.Add(param.Type.ResolvedType!);
        }

        Scope scope = CurrentScope();
        ReadOnlySpan<char> name = TokenValue(fd.NameToken);

        FuncType funcType = _typeRegistry.GetFuncType(returnType, paramTypes);
        FuncSymbol sym = new()
        {
            Declaration = fd,
            DeclaringScope = scope,
            Type = funcType,
            Name = name.ToString()
        };

        fd.Symbol = sym;

        // NOTE: Create symbol even if it's a redeclaration

        RegisterSymbol(sym, scope);
    }

    private void RegisterSymbol(Symbol symbol, Scope scope)
    {
        // TODO: Lookup once

        string name = symbol.Name;
        Symbol? loc = scope.LookupLocal(name);
        if (loc != null)
        {
            ErrorRedeclaration(symbol, loc);
            return;
        }

        Symbol? rec = scope.LookupRecursive(name);
        if (rec != null)
        {
            WarningShadow(symbol, rec);
        }

        bool ok = scope.TryDeclare(symbol);
        Debug.Assert(ok);
    }

    private void ResolveType(TypeDecl typeDecl)
    {
        Debug.Assert(typeDecl.ResolvedType == null);

        ReadOnlySpan<char> name = TokenValue(typeDecl.TypeNameToken);
        Symbol? sym = LookupRecursive(name);
        if (sym == null)
        {
            Error($"Type not found: {name}", typeDecl);
            typeDecl.ResolvedType = BuiltinType.Error;
            return;
        }

        TypeSymbol? typeSym = sym as TypeSymbol;
        if (typeSym == null)
        {
            Error($"Type expected: {name}. Given: {sym.GetType().Name}", typeDecl);
            typeDecl.ResolvedType = BuiltinType.Error;
            return;
        }

        typeDecl.ResolvedType = typeSym.Type;
    }

    private Symbol? LookupLocal(ReadOnlySpan<char> name)
    {
        return CurrentScope().LookupLocal(name);
    }

    private Symbol? LookupRecursive(ReadOnlySpan<char> name)
    {
        return CurrentScope().LookupRecursive(name);
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
        _diag.AddError(message, _tokens[node.StartToken]);
    }

    private void ErrorRedeclaration(Symbol newSymbol, Symbol oldSymbol)
    {
        Debug.Assert(newSymbol.Name == oldSymbol.Name);
        Debug.Assert(newSymbol.DeclaringNode != null);

        string message = $"Redeclaration of {newSymbol.Name}";
        if (oldSymbol.DeclaringNode != null)
        {
            int oldSymbolToken = oldSymbol.DeclaringNode.StartToken;
            Token old = _tokens[oldSymbolToken];
            message += $". Previously declared at {old.Line}:{old.Column}";
        }

        int newSymbolToken = newSymbol.DeclaringNode.StartToken;
        _diag.AddError(message, _tokens[newSymbolToken]);
    }

    private void WarningShadow(Symbol newSymbol, Symbol oldSymbol)
    {
        Debug.Assert(newSymbol.Name == oldSymbol.Name);
        Debug.Assert(newSymbol.DeclaringNode != null);
    }
}