namespace Compiler;

public class Symbol
{
    public required Type Type;
    public required string Name;
    public required Node Declaration;

    public int ScopeId = -1; // For debug

    public bool IsFunc => Type is FuncType;
    public bool IsBuiltin => Type is BuiltinType;
}