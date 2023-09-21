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
        private static TypeBuilder? typeBuilder;

        private static readonly TokenListParser<Token, TypeRef> TypeName
            = Superpower.Parsers.Token.EqualToValue(Token.Sym, "int").Value(TypeRef.INT)
              .Or(Superpower.Parsers.Token.EqualToValue(Token.Sym, "bool").Value(TypeRef.BOOL))
              .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(name => (TypeRef)typeBuilder!.GetClassType(name.ToStringValue())));

        private static readonly TokenListParser<Token, Expr> Atom =
            Superpower.Parsers.Token.EqualTo(Token.Int).Select(s => (Expr)new IntExpr(int.Parse(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(s => (Expr)new VarExpr(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.KwTrue).Value((Expr)new BoolLit(true)))
            .Or(Superpower.Parsers.Token.EqualTo(Token.KwFalse).Value((Expr)new BoolLit(false)))
            // `Lazy` must be used to add a level of indirection (because the `Expr` field is not initialized at the moment)
            .Or(Lazy(GetExpr).Between(Superpower.Parsers.Token.EqualTo(Token.LParen), Superpower.Parsers.Token.EqualTo(Token.RParen)));

        private static readonly TokenListParser<Token, Expr> Member = 
            Atom.Chain(Superpower.Parsers.Token.EqualTo(Token.Dot), Superpower.Parsers.Token.EqualTo(Token.Sym),
                (_, obj, name) => new MemberExpr(obj, name.ToStringValue()));

        private static readonly TokenListParser<Token, Expr[]> ExprArgs =
            Lazy(GetExpr).ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma));

        private static readonly TokenListParser<Token, Expr> Call =
            Member.Then(atom => Superpower.Parsers.Token.EqualTo(Token.LParen)
                                .IgnoreThen(ExprArgs)
                                .Then(args => Superpower.Parsers.Token.EqualTo(Token.RParen)
                                              .Value((Expr)new CallExpr(atom, args))
                                ).OptionalOrDefault(atom));

        private static readonly TokenListParser<Token, Expr> Factor =
            Call.Chain(Superpower.Parsers.Token.EqualTo(Token.Star), Call,
                (_, lhs, rhs) => new BinOpExpr(lhs, rhs, BinOpExpr.Ops.Mul));

        private static readonly TokenListParser<Token, Expr> AddSubExpr =
            Factor.Chain(Superpower.Parsers.Token.EqualTo(Token.Plus).Or(Superpower.Parsers.Token.EqualTo(Token.Minus)), Factor,
                (op, lhs, rhs) => new BinOpExpr(lhs, rhs,
                    op.ToStringValue() == "+" ? BinOpExpr.Ops.Add : op.ToStringValue() == "-" ? BinOpExpr.Ops.Sub : throw new Exception()));

        private static readonly TokenListParser<Token, Expr> CmpExpr =
            AddSubExpr.Then(lhs =>
                Superpower.Parsers.Token.EqualTo(Token.Equal).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpEq))
                .Or(Superpower.Parsers.Token.EqualTo(Token.NotEqual).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpNe)))
                .Or(Superpower.Parsers.Token.EqualTo(Token.Lower).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpLt)))
                .Or(Superpower.Parsers.Token.EqualTo(Token.LowerEqual).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpLe)))
                .Or(Superpower.Parsers.Token.EqualTo(Token.Greater).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpGt)))
                .Or(Superpower.Parsers.Token.EqualTo(Token.GreaterEqual).IgnoreThen(Lazy(GetExpr)).Select(rhs => (Expr)new BinOpExpr(lhs, rhs, BinOpExpr.Ops.CmpGe)))
                .OptionalOrDefault(lhs));

        private static TokenListParser<Token, Expr> GetExpr() { return CmpExpr; }

        private static readonly TokenListParser<Token, Stmt> LetStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwLet)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Select(s => s.ToStringValue().Trim())
            .Then(varName => Superpower.Parsers.Token.EqualTo(Token.Assign)
                            .IgnoreThen(GetExpr())
                            .Select(varValue => (Stmt)new LetAssignStmt(varName, varValue)))
            .Then(expr => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(expr));

        private static readonly TokenListParser<Token, Stmt> AssignOrDiscardStmt =
            GetExpr().Then(leftExpr =>
                Superpower.Parsers.Token.EqualTo(Token.Assign)
                .IgnoreThen(GetExpr())
                .Select(rightExpr => (Stmt)new AssignStmt(leftExpr, rightExpr))
                .OptionalOrDefault((Stmt)new DiscardStmt(leftExpr))
            )
            .Then(expr => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(expr));

        private static readonly TokenListParser<Token, Stmt> ReturnStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwReturn)
            .IgnoreThen(GetExpr())
            .Select(expr => (Stmt)new ReturnStmt(expr))
            .Then(expr => Superpower.Parsers.Token.EqualTo(Token.Nl).Value(expr));

        private static readonly TokenListParser<Token, Stmt> IfStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwIf)
            .IgnoreThen(GetExpr())
            .Then(cond => Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl)
                          .IgnoreThen(GetBlock())
                           .Then(thenBlock => Superpower.Parsers.Token.Sequence(Token.KwElse, Token.Colon, Token.Nl)
                                              .IgnoreThen(GetBlock())
                                              .OptionalOrDefault(Array.Empty<Stmt>())
                                              .Select(elseBlock => (Stmt)new IfStmt(cond, thenBlock, elseBlock))));

        private static readonly TokenListParser<Token, Stmt> ParseStmt =
            LetStmt.Or(ReturnStmt).Or(IfStmt).Or(AssignOrDiscardStmt);

        private static readonly TokenListParser<Token, Stmt[]> Block =
            ParseStmt.Many().Between(Superpower.Parsers.Token.EqualTo(Token.Indent), Superpower.Parsers.Token.EqualTo(Token.Dedent));

        private static TokenListParser<Token, Stmt[]> GetBlock() { return Block; }

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
                        .IgnoreThen(Block)
                        .Select(body => new Func(funcName.ToStringValue(), funcArgs, body, retType))
                        )
                    )
               );

        private static readonly TokenListParser<Token, (TypeRef, string)> ParseField =
            Superpower.Parsers.Token.EqualTo(Token.KwVal)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Then(fieldName => Superpower.Parsers.Token.EqualTo(Token.Colon)
                                .IgnoreThen(TypeName)
                                .Then(type => Superpower.Parsers.Token.EqualTo(Token.Nl)
                                              .Value((type, fieldName.ToStringValue()))));

        private static readonly TokenListParser<Token, ClassDef> ParseClassDef =
            Superpower.Parsers.Token.EqualTo(Token.KwClass)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Then(className =>
                Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl, Token.Indent)
                .IgnoreThen(ParseField.Many())
                .Then(fields => Superpower.Parsers.Token.EqualTo(Token.Dedent)
                                .Value(new ClassDef(className.ToStringValue(), fields))));

        private static readonly TokenListParser<Token, ast.Program> ParseProgram =
            Superpower.Parsers.Token.EqualTo(Token.Nl).Optional()
            .IgnoreThen(
                ParseClassDef.Select(x => (object)x).Or(ParseFuncDef.Select(x => (object)x))
                .Then(x => Superpower.Parsers.Token.EqualTo(Token.Nl).Optional().Value(x))
                .Many()
                .Select(xs => 
                {
                    var classes = xs.Where(x => x is ClassDef).Select(x => (ClassDef)x).ToArray();
                    var funcs = xs.Where(x => x is Func).Select(x => (Func)x).ToArray();
                    return new ast.Program(classes, funcs);
                }));

        public static ast.Program LexAndParse(string s)
        {
            var tokens = Lexer.TokenizeString(s);
            // ! this is important
            typeBuilder = new TypeBuilder();
            var result = ParseProgram(new Superpower.Model.TokenList<Token>(tokens.ToArray()));
            if (!result.HasValue)
                Console.WriteLine(result.FormatErrorMessageFragment());
            // ! now fill in the types
            typeBuilder.InitializeAll(result.Value.Classes);
            return result.Value;
        }

        public static TokenListParser<TKind, T> Lazy<TKind, T>(Func<TokenListParser<TKind, T>> parser)
        {
            return (tokens) => parser()(tokens);
        }
        
    }
}
