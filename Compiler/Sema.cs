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

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag, bool debug)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;
        _typeRegistry = new TypeRegistry();
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
        MarkSymbols(unit);
        PopScope();
    }

    // TODO: Bad name?
    private void MarkSymbols(CompilationUnit unit)
    {
        foreach (FuncDecl func in unit.FuncDecls)
        {
            PushScope();
            MarkSymbols(func);
            PopScope();
        }
    }

    private void MarkSymbols(FuncDecl func)
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

        MarkSymbols(func.Body);
    }

    private void MarkSymbols(Block block)
    {
        foreach (Stmt stmt in block.Stmts)
        {
            MarkSymbols(stmt);
        }
    }

    private void MarkSymbols(Stmt stmt)
    {
        switch (stmt)
        {
            case Block innerBlock:
                PushScope();
                MarkSymbols(innerBlock);
                PopScope();
                break;
            case StmtAssign stmtAssign:
                break;
            case StmtExpr stmtExpr:
                break;
            case StmtLet stmtLet:
                break;
            case StmtReturn stmtReturn:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stmt));
        }
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

    private bool HasSymbol(string name)
    {
        return _nameToSymbolIndex.ContainsKey(name);
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
