namespace Compiler;

public abstract class Node
{
    public required int StartToken;
    public required int EndToken; // inclusive
}

public class CompilationUnit : Node
{
    public required List<FuncDecl> FuncDecls;
}

public class FuncDecl : Node
{
    public required int NameToken;
    public required List<Param> Params;
    public required TypeDecl? ReturnType;
    public required Block Body;
}

public class Param : Node
{
    public required int NameToken;
    public required TypeDecl Type;
}

public class TypeDecl : Node
{
    public required int TypeNameToken;
}

public abstract class Stmt : Node
{
}

public class Block : Stmt
{
    public required List<Stmt> Stmts;
}

public class StmtLet : Stmt
{
    public required int NameToken;
    public required TypeDecl? Type;
    public required Expr? Expr;
}

public class StmtReturn : Stmt
{
    public required Expr? Expr;
}

public class StmtAssign : Stmt
{
    public required int AssignToken;
    public required Expr Target;
    public required Expr Value;
}

public class StmtExpr : Stmt
{
    public required Expr Expr;
}

public abstract class Expr : Node
{
}

public class ExprBinary : Expr
{
    public required int OperatorToken;
    public required Expr Left;
    public required Expr Right;
}

public class ExprUnary : Expr
{
    public required int OperatorToken;
    public required Expr Expr;
}

public abstract class ExprPrimary : Expr
{
}

public class ExprInt : ExprPrimary
{
    public required int LiteralToken;
}

public class ExprIdentifier : ExprPrimary
{
    public required int IdentifierToken;
}

public class ExprCall : ExprPrimary
{
    public required int IdentifierToken;
    public required List<Expr> Args;
}