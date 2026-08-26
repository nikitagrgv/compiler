using System.Diagnostics;

namespace Compiler;

public class Sema
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];
    private TypeRegistry _typeRegistry = new();

    private Dictionary<string, Symbol> _symbols = new();

    public bool Run(CompilationUnit unit, string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _hasErrors = false;
        _tokens = tokens;
        _typeRegistry = new TypeRegistry();
        _symbols.Clear();

        Run(unit);

        return _hasErrors;
    }

    private void Run(CompilationUnit unit)
    {
        CollectFunctions(unit);
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
            RegisterSymbol(name, sym);
        }
    }

    private void RegisterSymbol(string name, Symbol sym)
    {
        Debug.Assert(!HasSymbol(name));
        _symbols[name] = sym;
    }

    private bool HasSymbol(string name)
    {
        return _symbols.ContainsKey(name);
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
        foreach (KeyValuePair<string, Symbol> it in _symbols)
        {
            Symbol sym = it.Value;
            Console.WriteLine($"{sym.Name} | {sym.Type.Name}");
        }
    }
}