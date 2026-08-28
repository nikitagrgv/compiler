namespace Compiler;

public enum SymbolKind
{
    Func,
    Param,
    Local,
}

public class Symbol
{
    public required Type Type;
    public required SymbolKind Kind;
    public required string Name;
    public required Node Declaration;

    public int ScopeId = -1; // For debug
}