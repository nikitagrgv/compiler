namespace Compiler;

public class TypeRegistry
{
    private HashSet<FuncType> _funcTypes = new(FuncComparer.Instance);

    private class FuncComparer : IEqualityComparer<FuncType>
    {
        public static readonly FuncComparer Instance = new();

        public void GetFuncType(Type returnType, List<Type> paramTypes)
        {
            
        }

        public bool Equals(FuncType? x, FuncType? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            return x.ReturnType == y.ReturnType && x.ParamTypes.SequenceEqual(y.ParamTypes);
        }

        public int GetHashCode(FuncType obj)
        {
            HashCode hc = new();
            hc.Add(obj.ReturnType);
            foreach (Type t in obj.ParamTypes)
            {
                hc.Add(t);
            }

            return hc.ToHashCode();
        }
    }
}