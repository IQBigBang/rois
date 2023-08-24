using RoisLang.ast;
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
        private static Superpower.TokenListParser<RoisLang.parser.Token, Expr> Atom =>
            Superpower.Parsers.Token.EqualTo(Token.Int).Select(s => (Expr)new IntExpr(int.Parse(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(s => (Expr)new VarExpr(s.ToStringValue())));

        private static Superpower.TokenListParser<RoisLang.parser.Token, Expr> Expr =>
            Atom.Chain(Superpower.Parsers.Token.EqualTo(Token.Plus), Atom,
                (_op, lhs, rhs) => new BinOpExpr(lhs, rhs, BinOpExpr.Ops.Add));

        private static Superpower.TokenListParser<RoisLang.parser.Token, Stmt> LetStmt =>
            Superpower.Parsers.Token.EqualTo(Token.KwLet)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Select(s => s.ToStringValue().Trim())
            .Then(varName => Superpower.Parsers.Token.EqualTo(Token.Assign)
                            .IgnoreThen(Expr)
                            .Select(varValue => (Stmt)new LetAssignStmt(varName, varValue)));

        private static Superpower.TokenListParser<RoisLang.parser.Token, Stmt> AssignStmt =>
            Expr.Then(leftExpr =>
                Superpower.Parsers.Token.EqualTo(Token.Assign)
                .IgnoreThen(Expr)
                .Select(rightExpr => (Stmt)new AssignStmt(leftExpr, rightExpr))
            );

        private static Superpower.TokenListParser<RoisLang.parser.Token, Stmt> DiscardStmt =>
            Expr.Select(leftExpr => (Stmt)new DiscardStmt(leftExpr));

        private static Superpower.TokenListParser<RoisLang.parser.Token, Stmt> ParseStmt =>
            LetStmt.Or(AssignStmt).Or(DiscardStmt)/*.Then(stmt => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(stmt))*/;

        private static Superpower.TokenListParser<RoisLang.parser.Token, Stmt[]> ParseStmts =>
            ParseStmt.Then(stmt => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(stmt)).Many();
            //ParseStmt.ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Nl)/*, Superpower.Parsers.Token.EqualTo(Token.Nl).OptionalOrDefault()*/);

        private static Superpower.TokenListParser<RoisLang.parser.Token, string[]> FuncDefArgs =>
            Superpower.Parsers.Token.EqualTo(Token.Sym).Select(tok => tok.ToStringValue()).ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma));

        private static Superpower.TokenListParser<RoisLang.parser.Token, Func> ParseFuncDef =>
            Superpower.Parsers.Token.EqualTo(Token.KwDef)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Then(funcName =>
                Superpower.Parsers.Token.EqualTo(Token.LParen)
                .IgnoreThen(FuncDefArgs)
                .Then(funcArgs =>
                    Superpower.Parsers.Token.EqualTo(Token.RParen)
                    .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Colon))
                    .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Nl))
                    .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Indent))
                    .IgnoreThen(ParseStmts)
                    .Then(body =>
                        Superpower.Parsers.Token.EqualTo(Token.Dedent)
                        .Value(new Func(funcName.ToStringValue(), funcArgs, body))
                    )
                )
            );

        public static Func LexAndParse(string s)
        {
            var tokens = Lexer.TokenizeString(s);
            var result = ParseFuncDef(new Superpower.Model.TokenList<Token>(tokens.ToArray()));
            if (!result.HasValue)
                Console.WriteLine(result.FormatErrorMessageFragment());
            return result.Value;
        }
    }
}
