using System.Diagnostics;

namespace Compiler;

public class Parser
{
    private string _code = "";
    private Diagnostic? _diag;
    private bool _hasErrors = false;
    private List<Token> _tokens = [];
    private int _cursor;

    private class UnexpectedTokenException(Token given, TokenType? expected)
        : Exception($"Unexpected token: {given.Type}")
    {
        public Token GivenToken { get; init; } = given;
        public TokenType? Expected { get; init; } = expected;
    }

    public struct Result
    {
        public CompilationUnit CompilationUnit;
        public bool HasErrors;
    }

    public Result Run(string code, List<Token> tokens, Diagnostic diag)
    {
        _code = code;
        _diag = diag;
        _tokens = tokens;
        _hasErrors = false;
        _cursor = 0;

        CompilationUnit compilationUnit = ParseCompilationUnit();
        Result result = new()
        {
            CompilationUnit = compilationUnit,
            HasErrors = _hasErrors,
        };
        return result;
    }

    private Token Peek(int n = 0)
    {
        int pos = _cursor + n;
        if (pos >= _tokens.Count)
        {
            pos = _tokens.Count - 1;
        }

        return _tokens[pos];
    }

    private bool Check(TokenType type) => Peek().Type == type;
    private bool CheckNext(TokenType type) => Peek(1).Type == type;

    private bool TryConsume(TokenType type)
    {
        if (!Check(type))
        {
            return false;
        }

        Advance();
        return true;
    }

    private bool IsAtEnd() => Check(TokenType.Eof);

    private void Advance()
    {
        _cursor++;
        if (_cursor >= _tokens.Count)
        {
            _cursor = _tokens.Count - 1;
        }
    }

    private int Expect(TokenType type)
    {
        int pos = _cursor;
        if (!Check(type))
        {
            Token given = _tokens[_cursor];
            throw new UnexpectedTokenException(given, type);
        }

        Advance();
        return pos;
    }

    private bool GoTo(int prevCursor, params TokenType[] types)
    {
        Debug.Assert(types.Length > 0 && !types.Contains(TokenType.Eof));

        if (_cursor <= prevCursor)
        {
            Advance();
        }

        while (!IsAtEnd())
        {
            if (types.Contains(Peek().Type))
            {
                return true;
            }

            Advance();
        }

        return false;
    }

    private int End(int begin)
    {
        return Math.Max(begin, _cursor - 1);
    }

    private CompilationUnit ParseCompilationUnit()
    {
        int begin = _cursor;

        List<FuncDecl> funcDecls = [];
        while (!IsAtEnd())
        {
            int prevCursor = _cursor;
            try
            {
                FuncDecl decl = ParseFuncDecl();
                funcDecls.Add(decl);
            }
            catch (UnexpectedTokenException e)
            {
                ReportError(e);
                GoTo(prevCursor, TokenType.KeywordFunc);
            }
        }

        int end = End(begin);
        CompilationUnit unit = new()
        {
            StartToken = begin,
            EndToken = end,
            FuncDecls = funcDecls
        };
        return unit;
    }

    private FuncDecl ParseFuncDecl()
    {
        int begin = _cursor;

        Expect(TokenType.KeywordFunc);
        int nameToken = Expect(TokenType.Identifier);

        List<Param> parameters = [];
        Expect(TokenType.LPar);
        if (!Check(TokenType.RPar))
        {
            while (true)
            {
                int prevCursor = _cursor;
                try
                {
                    Param param = ParseParam();
                    parameters.Add(param);
                    if (Check(TokenType.RPar))
                    {
                        break;
                    }

                    Expect(TokenType.Comma);
                }
                catch (UnexpectedTokenException e)
                {
                    ReportError(e);
                    if (Check(TokenType.RPar))
                    {
                        break;
                    }

                    GoTo(prevCursor, TokenType.Comma, TokenType.RPar);
                    if (Check(TokenType.Comma))
                    {
                        Advance();
                        continue;
                    }

                    break;
                }
            }
        }

        Expect(TokenType.RPar);

        TypeDecl? returnType = null;
        if (TryConsume(TokenType.Colon))
        {
            returnType = ParseType();
        }

        Block body = ParseBlock();

        int end = End(begin);
        return new FuncDecl
        {
            StartToken = begin,
            EndToken = end,
            NameToken = nameToken,
            Params = parameters,
            ReturnType = returnType,
            Body = body,
        };
    }

    private Param ParseParam()
    {
        int begin = _cursor;
        int nameToken = Expect(TokenType.Identifier);
        Expect(TokenType.Colon);
        TypeDecl typeDecl = ParseType();
        int end = End(begin);
        return new Param
        {
            StartToken = begin,
            EndToken = end,
            NameToken = nameToken,
            Type = typeDecl,
        };
    }

    private TypeDecl ParseType()
    {
        int begin = _cursor;
        int typeNameToken = Expect(TokenType.Identifier);
        int end = End(begin);
        return new TypeDecl
        {
            StartToken = begin,
            EndToken = end,
            TypeNameToken = typeNameToken,
        };
    }

    private Block ParseBlock()
    {
        int begin = _cursor;
        List<Stmt> stmts = [];

        Expect(TokenType.LBrace);

        while (!Check(TokenType.RBrace) && !IsAtEnd())
        {
            int prevCursor = _cursor;
            try
            {
                Stmt stmt = ParseStmt();
                stmts.Add(stmt);
            }
            catch (UnexpectedTokenException e)
            {
                ReportError(e);
                GoTo(prevCursor,
                    TokenType.Semicolon,
                    TokenType.KeywordLet,
                    TokenType.KeywordReturn,
                    TokenType.LBrace,
                    TokenType.RBrace
                );
                if (Check(TokenType.Semicolon))
                {
                    Advance();
                }
            }
        }

        Expect(TokenType.RBrace);

        int end = End(begin);
        return new Block
        {
            StartToken = begin,
            EndToken = end,
            Stmts = stmts
        };
    }

    private Stmt ParseStmt()
    {
        if (Check(TokenType.LBrace))
        {
            return ParseBlock();
        }

        if (Check(TokenType.KeywordLet))
        {
            return ParseLet();
        }

        if (Check(TokenType.KeywordReturn))
        {
            return ParseReturn();
        }

        return ParseAssignOrStmtExpr();
    }

    private StmtLet ParseLet()
    {
        int begin = _cursor;
        Expect(TokenType.KeywordLet);
        int nameToken = Expect(TokenType.Identifier);

        TypeDecl? typeDecl = null;
        if (TryConsume(TokenType.Colon))
        {
            typeDecl = ParseType();
        }

        Expr? expr = null;
        if (typeDecl == null)
        {
            Expect(TokenType.Assign);
            expr = ParseExpr();
        }
        else if (TryConsume(TokenType.Assign))
        {
            expr = ParseExpr();
        }

        Expect(TokenType.Semicolon);

        int end = End(begin);
        return new StmtLet
        {
            StartToken = begin,
            EndToken = end,
            NameToken = nameToken,
            Type = typeDecl,
            Expr = expr
        };
    }

    private StmtReturn ParseReturn()
    {
        int begin = _cursor;
        Expr? expr = null;

        Expect(TokenType.KeywordReturn);
        if (!Check(TokenType.Semicolon))
        {
            expr = ParseExpr();
        }

        Expect(TokenType.Semicolon);

        int end = End(begin);
        return new StmtReturn
        {
            StartToken = begin,
            EndToken = end,
            Expr = expr
        };
    }

    private Stmt ParseAssignOrStmtExpr()
    {
        int begin = _cursor;
        Expr baseExp = ParseExpr();

        Stmt stmt;
        if (Check(TokenType.Assign))
        {
            int op = _cursor;
            Advance();

            Expr value = ParseExpr();
            Expect(TokenType.Semicolon);

            int end = End(begin);
            stmt = new StmtAssign
            {
                StartToken = begin,
                EndToken = end,
                AssignToken = op,
                Target = baseExp,
                Value = value
            };
        }
        else
        {
            Expect(TokenType.Semicolon);

            int end = End(begin);
            stmt = new StmtExpr
            {
                StartToken = begin,
                EndToken = end,
                Expr = baseExp
            };
        }

        return stmt;
    }

    // TODO: Use precedence parsing
    private Expr ParseExpr()
    {
        int begin = _cursor;
        Expr left = ParseTerm();
        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            int op = _cursor;
            Advance();

            Expr right = ParseTerm();
            int end = End(begin);

            left = new ExprBinary
            {
                StartToken = begin,
                EndToken = end,
                Left = left,
                Right = right,
                OperatorToken = op
            };
        }

        return left;
    }

    private Expr ParseTerm()
    {
        int begin = _cursor;
        Expr left = ParseUnary();
        while (Check(TokenType.Star) ||
               Check(TokenType.Slash) ||
               Check(TokenType.Percent))
        {
            int op = _cursor;
            Advance();

            Expr right = ParseUnary();
            int end = End(begin);

            left = new ExprBinary
            {
                StartToken = begin,
                EndToken = end,
                Left = left,
                Right = right,
                OperatorToken = op
            };
        }

        return left;
    }

    private Expr ParseUnary()
    {
        int begin = _cursor;
        if (TryConsume(TokenType.Plus) || TryConsume(TokenType.Minus))
        {
            Expr expr = ParseUnary();
            int end = End(begin);
            return new ExprUnary
            {
                StartToken = begin,
                EndToken = end,
                OperatorToken = begin,
                Expr = expr
            };
        }

        return ParsePrimary();
    }

    private Expr ParsePrimary()
    {
        int begin = _cursor;
        if (TryConsume(TokenType.LiteralInt))
        {
            int end = End(begin);
            return new ExprInt
            {
                StartToken = begin,
                EndToken = end,
                LiteralToken = begin,
            };
        }

        if (TryConsume(TokenType.LPar))
        {
            Expr expr = ParseExpr();
            Expect(TokenType.RPar);
            return expr;
        }

        if (Check(TokenType.Identifier))
        {
            if (CheckNext(TokenType.LPar))
            {
                return ParseCall();
            }

            return ParseIdentifier();
        }

        Token token = _tokens[_cursor];
        throw new UnexpectedTokenException(token, null);
    }

    private Expr ParseIdentifier()
    {
        int begin = _cursor;
        int identifierToken = Expect(TokenType.Identifier);
        int end = End(begin);
        return new ExprIdentifier
        {
            StartToken = begin,
            EndToken = end,
            IdentifierToken = identifierToken
        };
    }

    private Expr ParseCall()
    {
        int begin = _cursor;

        int identifierToken = Expect(TokenType.Identifier);

        List<Expr> args = [];
        Expect(TokenType.LPar);
        if (!Check(TokenType.RPar))
        {
            do
            {
                Expr expr = ParseExpr();
                args.Add(expr);
            } while (TryConsume(TokenType.Comma));
        }

        Expect(TokenType.RPar);

        int end = End(begin);
        return new ExprCall
        {
            StartToken = begin,
            EndToken = end,
            IdentifierToken = identifierToken,
            Args = args
        };
    }

    private void ReportError(UnexpectedTokenException e)
    {
        _hasErrors = true;
        string message = UnexpectedTokenMessage(e.GivenToken, e.Expected);
        _diag?.AddError(message,
            e.GivenToken.Position,
            e.GivenToken.Length,
            e.GivenToken.Line,
            e.GivenToken.Column);
    }

    private string UnexpectedTokenMessage(Token given, TokenType? expected = null)
    {
        string value = given.Value(_code).ToString();
        if (value.Length <= 0)
        {
            value = given.Type.PrettyName();
        }

        string str = $"Unexpected token: {value}";
        if (expected != null)
        {
            str += ". Expected " + expected.Value.ErrorMessageName();
        }

        return str;
    }
}