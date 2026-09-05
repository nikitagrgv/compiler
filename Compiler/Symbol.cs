namespace Compiler;

// TODO: Merge subtypes to single Symbol class?
public abstract class Symbol
{
    public required string Name { get; init; }
    public required Scope DeclaringScope { get; init; }
    public required Type Type { get; init; }

    public override string ToString()
    {
        string type = GetType().Name;
        return $"${Name}({type})";
    }
}

public sealed class VariableSymbol : Symbol
{
    public required StmtLet Declaration { get; init; }
}

public sealed class ParamSymbol : Symbol
{
    public required Param Declaration { get; init; }
}

public sealed class FuncSymbol : Symbol
{
    public required FuncDecl Declaration { get; init; }
}

public sealed class TypeSymbol : Symbol
{
    // Will be added with user types, e.g., structs
    // public required Node? Declaration { get; init; }
}