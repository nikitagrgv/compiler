namespace Compiler;

public class TypeRegistry
{
    private struct FuncSignature : IEquatable<FuncSignature>
    {
        public Type ReturnType;
        public IReadOnlyList<Type> ParamTypes;

        public bool Equals(FuncSignature other)
        {
            return ReturnType == other.ReturnType && ParamTypes.SequenceEqual(other.ParamTypes);
        }

        public override bool Equals(object? obj) => obj is FuncSignature other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hc = new();
            hc.Add(ReturnType);
            foreach (Type t in ParamTypes)
            {
                hc.Add(t);
            }

            return hc.ToHashCode();
        }
    }

    private readonly Dictionary<FuncSignature, FuncType> _funcTypes = new();

    public FuncType GetFuncType(Type returnType, List<Type> paramTypes)
    {
        FuncSignature signature = new()
        {
            ReturnType = returnType,
            ParamTypes = [.. paramTypes],
        };

        if (_funcTypes.TryGetValue(signature, out FuncType? type))
        {
            return type;
        }

        type = new FuncType
        {
            ReturnType = signature.ReturnType,
            ParamTypes = signature.ParamTypes,
        };
        _funcTypes.Add(signature, type);
        return type;
    }
}