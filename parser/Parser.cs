using RoisLang.ast;
using RoisLang.types;
using Superpower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.parser
{
    internal class Parser
    {
        private static readonly TokenListParser<Token, TypeRef> TypeName
            = Superpower.Parsers.Token.EqualToValue(Token.Sym, "int").Value(TypeRef.INT);

        private static readonly TokenListParser<Token, Expr> Atom =
            Superpower.Parsers.Token.EqualTo(Token.Int).Select(s => (Expr)new IntExpr(int.Parse(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(s => (Expr)new VarExpr(s.ToStringValue())))
            // `Lazy` must be used to add a level of indirection (because the `Expr` field is not initialized at the moment)
            .Or(Lazy(GetExpr).Between(Superpower.Parsers.Token.EqualTo(Token.LParen), Superpower.Parsers.Token.EqualTo(Token.RParen)));

        private static readonly TokenListParser<Token, Expr> Call =
            Atom.Then(atom => Superpower.Parsers.Token.EqualTo(Token.LParen)
                               .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.RParen))
                                .Value((Expr)new CallExpr(atom))
                                .OptionalOrDefault(atom));

        private static readonly TokenListParser<Token, Expr> Expr =
            Call.Chain(Superpower.Parsers.Token.EqualTo(Token.Plus).Or(Superpower.Parsers.Token.EqualTo(Token.Minus)), Call,
                (op, lhs, rhs) => new BinOpExpr(lhs, rhs,
                    op.ToStringValue() == "+" ? BinOpExpr.Ops.Add : op.ToStringValue() == "-" ? BinOpExpr.Ops.Sub : throw new Exception()));

        private static TokenListParser<Token, Expr> GetExpr() { return Expr; }

        private static readonly TokenListParser<Token, Stmt> LetStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwLet)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Select(s => s.ToStringValue().Trim())
            .Then(varName => Superpower.Parsers.Token.EqualTo(Token.Assign)
                            .IgnoreThen(Expr)
                            .Select(varValue => (Stmt)new LetAssignStmt(varName, varValue)));

        private static readonly TokenListParser<Token, Stmt> AssignOrDiscardStmt =
            Expr.Then(leftExpr =>
                Superpower.Parsers.Token.EqualTo(Token.Assign)
                .IgnoreThen(Expr)
                .Select(rightExpr => (Stmt)new AssignStmt(leftExpr, rightExpr))
                .OptionalOrDefault((Stmt)new DiscardStmt(leftExpr))
            );

        private static readonly TokenListParser<Token, Stmt> ReturnStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwReturn)
            .IgnoreThen(Expr)
            .Select(expr => (Stmt)new ReturnStmt(expr));

        private static readonly TokenListParser<Token, Stmt> ParseStmt =
            LetStmt.Or(ReturnStmt).Or(AssignOrDiscardStmt);

        private static TokenListParser<Token, Stmt[]> ParseStmts =
            ParseStmt.Then(stmt => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(stmt)).Many();
        //ParseStmt.ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Nl)/*, Superpower.Parsers.Token.EqualTo(Token.Nl).OptionalOrDefault()*/);

        private static readonly TokenListParser<Token, (string, TypeRef)[]> FuncDefArgs =
            Superpower.Parsers.Token.EqualTo(Token.Sym)
            .Then(argName => Superpower.Parsers.Token.EqualTo(Token.Colon)
                  .IgnoreThen(TypeName)
                  .Select(type => (argName.ToStringValue(), type)))
            .ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma));

        private static readonly TokenListParser<Token, TypeRef> RetType =
            Superpower.Parsers.Token.EqualTo(Token.Arrow)
            .IgnoreThen(TypeName)
            .OptionalOrDefault(TypeRef.VOID);

        private static readonly TokenListParser<Token, Func> ParseFuncDef =
            Superpower.Parsers.Token.EqualTo(Token.KwDef)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Then(funcName =>
                Superpower.Parsers.Token.EqualTo(Token.LParen)
                .IgnoreThen(FuncDefArgs)
                .Then(funcArgs =>
                    Superpower.Parsers.Token.EqualTo(Token.RParen)
                    .IgnoreThen(RetType)
                    .Then(retType => 
                         Superpower.Parsers.Token.EqualTo(Token.Colon)
                        .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Nl))
                        .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Indent))
                        .IgnoreThen(ParseStmts)
                        .Then(body =>
                            Superpower.Parsers.Token.EqualTo(Token.Dedent)
                            .Value(new Func(funcName.ToStringValue(), funcArgs, body, retType))
                        )
                    )
               )
            );

        private static readonly TokenListParser<Token, ast.Program> ParseProgram =
            ParseFuncDef.Then(func => Superpower.Parsers.Token.EqualTo(Token.Nl).Optional().Value(func))
            .Many().Select(x => new ast.Program(x));

        public static ast.Program LexAndParse(string s)
        {
            var tokens = Lexer.TokenizeString(s);
            var result = ParseProgram(new Superpower.Model.TokenList<Token>(tokens.ToArray()));
            if (!result.HasValue)
                Console.WriteLine(result.FormatErrorMessageFragment());
            return result.Value;
        }

        public static TokenListParser<TKind, T> Lazy<TKind, T>(Func<TokenListParser<TKind, T>> parser)
        {
            return (tokens) => parser()(tokens);
        }
        
    }
}
