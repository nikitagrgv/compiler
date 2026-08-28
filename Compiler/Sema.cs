using System.Diagnostics;

namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];
    private TypeRegistry _typeRegistry = new();

    private bool _debug = false;

    private int _curScopeId = 0;
    private readonly List<Symbol> _allSymbols = new(); // For debug

    private readonly Dictionary<string, int> _nameToSymbolIndex = new();
    private readonly List<Symbol> _symbolsStack = new();
    private readonly List<int> _symbolsScopeCounts = new();
    private readonly List<Symbol> _functionsScope = new();

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag, bool debug)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;
        _typeRegistry = new TypeRegistry();
        _functionsScope.Clear();
        _debug = debug;

        _curScopeId = 0;
        _allSymbols.Clear();
        _nameToSymbolIndex.Clear();
        _symbolsStack.Clear();
        _symbolsScopeCounts.Clear();

        Run(unit);

        return _hasErrors;
    }

    private void Run(CompilationUnit unit)
    {
        PushScope();
        CollectFunctionsSymbols(unit);
        Visit(unit);
        PopScope();
    }

    private void Visit(CompilationUnit unit)
    {
        foreach (FuncDecl func in unit.FuncDecls)
        {
            Debug.Assert(func.Symbol != null, "Must be already parsed in CollectFunctionsSymbols");

            Symbol funcSymbol = func.Symbol;
            _functionsScope.Add(funcSymbol);
            PushScope();
            Visit(func);
            PopScope();

            Debug.Assert(_functionsScope.Last() == funcSymbol);

            _functionsScope.RemoveAt(_functionsScope.Count - 1);
        }
    }

    private void Visit(FuncDecl func)
    {
        foreach (Param param in func.Params)
        {
            string name = GetTokenValue(param.NameToken);
            if (HasSymbol(name))
            {
                ReportSymbolRedefinition(name, param.NameToken);
                continue;
            }

            Type type = TypeFromDecl(param.Type);
            Symbol sym = new()
            {
                Type = type,
                Name = name,
                Declaration = param,
            };
            RegisterSymbol(sym);
        }

        Visit(func.Body);
    }

    private void Visit(Block block)
    {
        foreach (Stmt stmt in block.Stmts)
        {
            Visit(stmt);
        }
    }

    private void Visit(Stmt stmt)
    {
        switch (stmt)
        {
            case Block block:
                PushScope();
                Visit(block);
                PopScope();
                break;
            case StmtAssign s:
                Visit(s);
                break;
            case StmtExpr s:
                Visit(s);
                break;
            case StmtLet s:
                Visit(s);
                break;
            case StmtReturn s:
                Visit(s);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stmt));
        }
    }

    private void Visit(StmtAssign stmt)
    {
    }

    private void Visit(StmtExpr stmt)
    {
    }

    private void Visit(StmtLet stmt)
    {
        Debug.Assert(stmt.Type != null || stmt.Expr != null, "Parser must have already checked this");

        string name = GetTokenValue(stmt.NameToken);
        if (HasSymbol(name))
        {
            ReportSymbolRedefinition(name, stmt.NameToken);
            return;
        }

        Type? declType = null;
        if (stmt.Type != null)
        {
            declType = TypeFromDecl(stmt.Type);
        }

        if (stmt.Expr != null)
        {
            Visit(stmt.Expr);
            if (declType != null)
            {
                stmt.Expr = Adapt(stmt.Expr, declType);
                Debug.Assert(stmt.Expr.Type == declType);
            }
            else
            {
                declType = stmt.Expr.Type;
            }
        }

        Debug.Assert(declType != null);
        Symbol sym = new()
        {
            Type = declType,
            Name = name,
            Declaration = stmt,
        };

        // NOTE: Only register after visiting the expression! So the expression can't use the symbol
        RegisterSymbol(sym);
    }

    private void Visit(StmtReturn stmt)
    {
        Type targetType = GetCurrentFunctionType().ReturnType;
    }

    private void Visit(Expr expr)
    {
        switch (expr)
        {
            case ExprBinary e:
                Visit(e);
                break;
            case ExprCall e:
                Visit(e);
                break;
            case ExprIdentifier e:
                Visit(e);
                break;
            case ExprInt e:
                Visit(e);
                break;
            case ExprUnary e:
                Visit(e);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expr));
        }
    }

    private void Visit(ExprBinary expr)
    {
    }

    private void Visit(ExprCall expr)
    {
        ExprIdentifier? identifier = expr.Callee as ExprIdentifier;
        if (identifier == null)
        {
            // Only function calls are supported for now
            throw new NotImplementedException();
        }

        Visit(identifier);
        Symbol? sym = identifier.Symbol;
        if (sym == null)
        {
            // Error already reported, just return
            expr.Type = BuiltinType.Error;
            return;
        }

        FuncType? funcType = sym.Type as FuncType;
        if (funcType == null)
        {
            Token token = _tokens[identifier.IdentifierToken];
            _diag?.AddError($"Callee must be a function, got type: {sym.Type}", token);
            _hasErrors = true;
            return;
        }

        expr.Type = funcType.ReturnType;
    }

    private void Visit(ExprIdentifier expr)
    {
        Debug.Assert(expr.Symbol == null);

        string name = GetTokenValue(expr.IdentifierToken);
        expr.Symbol = TryGetSymbol(name);
        if (expr.Symbol == null)
        {
            ReportSymbolNotFound(expr.IdentifierToken);
            expr.Type = BuiltinType.Error;
            return;
        }

        expr.Type = expr.Symbol.Type;
    }

    private void Visit(ExprInt expr)
    {
        expr.Type = BuiltinType.I32;
    }

    private void Visit(ExprUnary expr)
    {
    }

    private Expr Adapt(Expr expr, Type targetType)
    {
        Type? exprType = expr.Type;
        Debug.Assert(exprType != null, "Must be already parsed in Visit");

        if (exprType == targetType)
        {
            return expr;
        }

        // TODO: Generate implicit cast nodes
        throw new NotImplementedException();
    }

    private void CollectFunctionsSymbols(CompilationUnit unit)
    {
        foreach (FuncDecl funcDecl in unit.FuncDecls)
        {
            string name = GetTokenValue(funcDecl.NameToken);
            if (HasSymbol(name))
            {
                ReportSymbolRedefinition(name, funcDecl.NameToken);
                continue;
            }

            FuncType funcType = GetFuncType(funcDecl);
            Symbol sym = new()
            {
                Type = funcType,
                Name = name,
                Declaration = funcDecl,
            };
            funcDecl.Symbol = sym;

            RegisterSymbol(sym);
        }
    }

    private void PushScope()
    {
        _curScopeId++;
        _symbolsScopeCounts.Add(0);
    }

    private void PopScope()
    {
        Debug.Assert(_symbolsScopeCounts.Count > 0);
        Debug.Assert(_symbolsScopeCounts.Sum() == _symbolsStack.Count);

        int lastScopeIndex = _symbolsScopeCounts.Count - 1;
        int countSymbolsInScope = _symbolsScopeCounts[lastScopeIndex];
        while (countSymbolsInScope > 0)
        {
            Symbol sym = _symbolsStack.Last();
            Debug.Assert(_nameToSymbolIndex.ContainsKey(sym.Name));

            _symbolsStack.RemoveAt(_symbolsStack.Count - 1);
            _nameToSymbolIndex.Remove(sym.Name);

            countSymbolsInScope--;
        }

        _symbolsScopeCounts.RemoveAt(lastScopeIndex);

        Debug.Assert(_symbolsScopeCounts.Sum() == _symbolsStack.Count);
    }

    private void RegisterSymbol(Symbol sym)
    {
        Debug.Assert(!_nameToSymbolIndex.ContainsKey(sym.Name));
        Debug.Assert(_symbolsScopeCounts.Count > 0);

        sym.ScopeId = _curScopeId;

        int symbolIndex = _symbolsStack.Count;
        int lastScope = _symbolsScopeCounts.Count - 1;
        _symbolsStack.Add(sym);
        _symbolsScopeCounts[lastScope]++;
        _nameToSymbolIndex[sym.Name] = symbolIndex;

        if (_debug)
        {
            _allSymbols.Add(sym);
        }
    }

    private Symbol? TryGetSymbol(string name)
    {
        bool ok = _nameToSymbolIndex.TryGetValue(name, out int index);
        if (!ok)
        {
            return null;
        }

        return _symbolsStack[index];
    }

    private bool HasSymbol(string name)
    {
        return _nameToSymbolIndex.ContainsKey(name);
    }

    private FuncType GetCurrentFunctionType()
    {
        Debug.Assert(_functionsScope.Count > 0);
        Type type = _functionsScope.Last().Type;
        return (FuncType)type;
    }

    private FuncType GetFuncType(FuncDecl funcDecl)
    {
        Type returnType = BuiltinType.Void;
        if (funcDecl.ReturnType != null)
        {
            returnType = TypeFromDecl(funcDecl.ReturnType);
        }

        // TODO: Reuse list
        List<Type> parameters = new();
        foreach (Param param in funcDecl.Params)
        {
            Type paramType = TypeFromDecl(param.Type);
            parameters.Add(paramType);
        }

        FuncType funcType = _typeRegistry.GetFuncType(returnType, parameters);
        return funcType;
    }

    private Type TypeFromDecl(TypeDecl typeDecl)
    {
        Type? rt = TryFindTypeFromDecl(typeDecl);
        if (rt != null)
        {
            return rt;
        }

        ReportTypeNotFound(typeDecl);

        return BuiltinType.Error;
    }

    private Type? TryFindTypeFromDecl(TypeDecl typeDecl)
    {
        Token token = _tokens[typeDecl.TypeNameToken];
        ReadOnlySpan<char> value = token.Value(_code);
        return _typeRegistry.GetTypeByName(value);
    }

    private string GetTokenValue(int tokenIndex)
    {
        Token token = _tokens[tokenIndex];
        ReadOnlySpan<char> value = token.Value(_code);
        return value.ToString();
    }

    private void ReportSymbolRedefinition(string name, int nameToken)
    {
        Token token = _tokens[nameToken];
        _diag?.AddError($"Symbol redefinition: {name}", token);
        _hasErrors = true;
    }

    private void ReportSymbolNotFound(int nameToken)
    {
        Token token = _tokens[nameToken];
        _diag?.AddError($"Symbol not found: {token.Value(_code)}", token);
        _hasErrors = true;
    }

    private void ReportTypeNotFound(TypeDecl funcDeclReturnType)
    {
        Token token = _tokens[funcDeclReturnType.TypeNameToken];
        _diag?.AddError($"Type not found: {token.Value(_code)}", token);
        _hasErrors = true;
    }

    // TODO: Move out of here?
    public void PrintDebug()
    {
        Console.WriteLine("Symbols:");
        foreach (Symbol sym in _allSymbols)
        {
            Token startToken = _tokens[sym.Declaration.StartToken];
            string str = $"{startToken.Line,5}:{startToken.Column,-2} | {sym.Name,-10} | {sym.Type.Name,-26}";
            if (sym.ScopeId != 0)
            {
                str += $" | Scope ID: {sym.ScopeId}";
            }

            Console.WriteLine(str);
        }
    }
}