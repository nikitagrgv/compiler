namespace Compiler;

public abstract class Node
{
    public required int StartToken { get; init; }
    public required int EndToken { get; init; } // inclusive
}

public class CompilationUnit : Node
{
    public required List<FuncDecl> FuncDecls { get; init; }
}

public class FuncDecl : Node
{
    public required int NameToken { get; init; }
    public required List<Param> Params { get; init; }
    public required TypeDecl? ReturnType { get; init; }
    public required Block Body { get; init; }

    public FunctionSymbol? Symbol { get; set; }
}

public class Param : Node
{
    public required int NameToken { get; init; }
    public required TypeDecl Type { get; init; }
}

public class TypeDecl : Node
{
    public required int TypeNameToken { get; init; }
}

public abstract class Stmt : Node
{
}

public class Block : Stmt
{
    public required List<Stmt> Stmts { get; init; }
}

public class StmtLet : Stmt
{
    public required int NameToken { get; init; }
    public required TypeDecl? Type { get; init; }
    public required Expr? Expr { get; set; }
}

public class StmtReturn : Stmt
{
    public required Expr? Expr { get; set; }
}

public class StmtAssign : Stmt
{
    public required int AssignToken { get; init; }
    public required Expr Target { get; init; }
    public required Expr Value { get; set; }
}

public class StmtExpr : Stmt
{
    public required Expr Expr { get; init; }
}

public abstract class Expr : Node
{
    public Type? Type { get; set; }
}

public class ExprBinary : Expr
{
    public required int OperatorToken { get; init; }
    public required Expr Left { get; set; }
    public required Expr Right { get; set; }
}

public class ExprUnary : Expr
{
    public required int OperatorToken { get; init; }
    public required Expr Expr { get; init; }
}

public abstract class ExprPrimary : Expr
{
}

public class ExprInt : ExprPrimary
{
    public required int LiteralToken { get; init; }
}

public class ExprIdentifier : ExprPrimary
{
    public required int IdentifierToken { get; init; }

    public Symbol? Symbol { get; set; }
}

public class ExprCall : ExprPrimary
{
    public required Expr Callee { get; init; }
    public required List<Expr> Args { get; init; }
}