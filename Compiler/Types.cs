namespace Compiler;

public abstract class Type
{
    public abstract string Name { get; }
}

public class BuiltinType : Type
{
    public override string Name { get; }

    private BuiltinType(string name)
    {
        Name = name;
    }

    public static readonly BuiltinType I32 = new("i32");
    public static readonly BuiltinType Void = new("void");
    public static readonly BuiltinType Error = new("<error>");
}

public sealed class FuncType : Type
{
    public required IReadOnlyList<Type> ParamTypes { get; init; }
    public required Type ReturnType { get; init; }

    public override string Name => $"({string.Join(", ", ParamTypes.Select(t => t.Name))}): {ReturnType.Name}";
}