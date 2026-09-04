namespace Compiler;

public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new();

    public Scope? Parent { get; init; }

    public Symbol? LookupLocal(string name)
    {
        return _symbols.GetValueOrDefault(name);
    }

    public Symbol? LookupRecursive(string name)
    {
        Scope? cur = this;
        while (cur != null)
        {
            Symbol? s = cur.LookupLocal(name);
            if (s != null)
            {
                return s;
            }

            cur = cur.Parent;
        }

        return null;
    }

    public bool TryDeclare(Symbol symbol)
    {
        bool added = _symbols.TryAdd(symbol.Name, symbol);
        return added;
    }
}