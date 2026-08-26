using System.Diagnostics;

namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];
    private TypeRegistry _typeRegistry = new();

    private readonly List<Symbol> _allSymbols = new(); // For debug
    private readonly Dictionary<string, int> _nameToSymbolIndex = new();
    private readonly List<Symbol> _symbolsStack = new();
    private readonly List<int> _symbolsScopeCounts = new();

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;
        _typeRegistry = new TypeRegistry();

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
        CollectFunctions(unit);
        PopScope();
    }

    private void CollectFunctions(CompilationUnit unit)
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
                Name = name
            };
            RegisterSymbol(sym);
        }
    }

    private void PushScope()
    {
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

        int symbolIndex = _symbolsStack.Count;
        int lastScope = _symbolsScopeCounts.Count - 1;
        _symbolsStack.Add(sym);
        _symbolsScopeCounts[lastScope]++;
        _nameToSymbolIndex[sym.Name] = symbolIndex;

        _allSymbols.Add(sym);
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
            returnType = TypeFromDecl(funcDecl.ReturnType, out bool _);
        }

        // TODO: Reuse list
        List<Type> parameters = new();
        foreach (Param param in funcDecl.Params)
        {
            Type paramType = TypeFromDecl(param.Type, out bool _);
            parameters.Add(paramType);
        }

        FuncType funcType = _typeRegistry.GetFuncType(returnType, parameters);
        return funcType;
    }

    private Type TypeFromDecl(TypeDecl typeDecl, out bool ok)
    {
        Type? rt = TryFindTypeFromDecl(typeDecl);
        if (rt != null)
        {
            ok = true;
            return rt;
        }

        ReportTypeNotFound(typeDecl);

        ok = false;
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


    // TODO: Move out of here
    public void PrintDebug()
    {
        Console.WriteLine("Symbols:");
        foreach (Symbol sym in _allSymbols)
        {
            Token startToken = _tokens[sym.Declaration.StartToken];
            Console.WriteLine($"{startToken.Length}:{startToken.Column} | {sym.Name} | {sym.Type.Name}");
        }
    }
}