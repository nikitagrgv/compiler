namespace Compiler;

// TODO: Merge subtypes to single Symbol class?
public abstract class Symbol
{
    public required string Name { get; init; }
    public required Scope DeclaringScope { get; init; }
    public required Type Type;
}

public sealed class VariableSymbol : Symbol
{
    public required StmtLet Declaration { get; init; }
}

public sealed class ParamSymbol : Symbol
{
    public required Param Declaration { get; init; }
}

public sealed class FunctionSymbol : Symbol
{
    public required FuncDecl Declaration { get; init; }
}