namespace Compiler;

public class Symbol
{
    public required Type Type;
    public required string Name;

    public bool IsFunc => Type is FuncType;
    public bool IsBuiltin => Type is BuiltinType;
}