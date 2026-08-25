using System.Diagnostics.CodeAnalysis;

namespace Compiler;

public class TypeRegistry
{
    private struct FuncSignature : IEquatable<FuncSignature>
    {
        public Type ReturnType;
        public List<Type> ParamTypes;

        public bool Equals(FuncSignature other)
        {
            return ReturnType == other.ReturnType && ParamTypes.SequenceEqual(other.ParamTypes);
        }

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
            ParamTypes = paramTypes,
        };

        if (_funcTypes.TryGetValue(signature, out FuncType? type))
        {
            return type;
        }

        type = new FuncType()
        {
            ReturnType = returnType,
            ParamTypes = paramTypes,
        };
        _funcTypes.Add(signature, type);
        return type;
    }
}