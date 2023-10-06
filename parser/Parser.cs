using RoisLang.ast;
using RoisLang.types;
using RoisLang.utils;
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
        private TypeBuilder typeBuilder;
        private static Parser? instance;

        public Parser(TypeBuilder typeBuilder)
        {
            this.typeBuilder = typeBuilder;
        }

        private static readonly TokenListParser<Token, TypeRef> FunTypeName
            = Superpower.Parsers.Token.EqualTo(Token.KwFun)
              .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.LParen))
              .IgnoreThen(Lazy(GetTypeName).ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma)))
              .Then(args => Superpower.Parsers.Token.EqualTo(Token.RParen)
                            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Arrow)
                                        .IgnoreThen(Lazy(GetTypeName))
                                        .OptionalOrDefault(TypeRef.VOID))
                            .Select(ret => (TypeRef)FuncType.New(args.ToList(), ret)));

        private static TokenListParser<Token, TypeRef> GetTypeName() { return TypeName; }

        private static readonly TokenListParser<Token, TypeRef> TypeName
            = Superpower.Parsers.Token.EqualToValue(Token.Sym, "int").Value(TypeRef.INT)
              .Or(Superpower.Parsers.Token.EqualToValue(Token.Sym, "bool").Value(TypeRef.BOOL))
              .Or(Superpower.Parsers.Token.EqualToValue(Token.Sym, "ptr").Value(TypeRef.PTR))
              .Or(FunTypeName)
              .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(name => (TypeRef)instance!.typeBuilder.GetClassType(name.ToStringValue())));

        private static readonly TokenListParser<Token, KeyValuePair<string, Expr>> ConstructorArg =
            Superpower.Parsers.Token.EqualTo(Token.Sym)
            .Then(name => Superpower.Parsers.Token.EqualTo(Token.Colon)
                          .IgnoreThen(Lazy(GetExpr))
                          .Select(expr => KeyValuePair.Create(name.ToStringValue(), expr)));

        private static readonly TokenListParser<Token, Expr> Constructor =
            Superpower.Parsers.Token.EqualTo(Token.KwNew)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(name => instance!.typeBuilder.GetClassType(name.ToStringValue())))
            .Then(classType => Superpower.Parsers.Token.EqualTo(Token.LParen)
                        .IgnoreThen(ConstructorArg.ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma)))
                        .Then(arguments => Superpower.Parsers.Token.EqualTo(Token.RParen)
                                    .Value((Expr)new ConstructorExpr(classType, new Dictionary<string, Expr>(arguments)))));

        private static readonly TokenListParser<Token, Expr> Atom =
            Superpower.Parsers.Token.EqualTo(Token.Int).Select(s => (Expr)new IntExpr(int.Parse(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.Sym).Select(s => (Expr)new VarExpr(s.ToStringValue())))
            .Or(Superpower.Parsers.Token.EqualTo(Token.KwTrue).Value((Expr)new BoolLit(true)))
            .Or(Superpower.Parsers.Token.EqualTo(Token.KwFalse).Value((Expr)new BoolLit(false)))
            .Or(Superpower.Parsers.Token.EqualTo(Token.StrLit).Select(s => (Expr)new StrLit(s.ToStringValue()[1..^1])))
            .Or(Constructor)
            // `Lazy` must be used to add a level of indirection (because the `Expr` field is not initialized at the moment)
            .Or(Lazy(GetExpr).Between(Superpower.Parsers.Token.EqualTo(Token.LParen), Superpower.Parsers.Token.EqualTo(Token.RParen)));

        private static readonly TokenListParser<Token, Expr[]> ExprArgs =
             Lazy(GetExpr).ManyDelimitedBy(Superpower.Parsers.Token.EqualTo(Token.Comma));

        private static readonly TokenListParser<Token, Expr> Member =
            Atom.Chain(Superpower.Parsers.Token.EqualTo(Token.Dot), 
                Superpower.Parsers.Token.EqualTo(Token.Sym)
                .Then(name => Superpower.Parsers.Token.EqualTo(Token.LParen)
                                .IgnoreThen(ExprArgs)
                                .Then(args => Superpower.Parsers.Token.EqualTo(Token.RParen).Value((Expr[]?)args))
                                .OptionalOrDefault()
                                .Select(args => Tuple.Create(name.ToStringValue(), args)))
                , (_, obj, snd) => snd.Item2 == null ? new MemberExpr(obj, snd.Item1)
                                                     : new MethodCallExpr(obj, snd.Item1, snd.Item2));

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
            .Then(expr => Superpower.Parsers.Token.EqualTo(Token.Nl).Many().Value(expr));

        private static readonly TokenListParser<Token, Stmt> ReturnStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwReturn)
            .IgnoreThen(GetExpr())
            .Select(expr => (Stmt)new ReturnStmt(expr))
            .Then(expr => Superpower.Parsers.Token.EqualTo(Token.Nl).Many().Value(expr));

        private static readonly TokenListParser<Token, (Expr, Stmt[])> ElseIf =
            Superpower.Parsers.Token.Sequence(Token.KwElse, Token.KwIf).Try()
            .IgnoreThen(GetExpr())
            .Then(cond => Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl)
                          .IgnoreThen(GetBlock()).Select(block => (cond, block)));

        private static readonly TokenListParser<Token, Stmt> ParseIfStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwIf)
            .IgnoreThen(GetExpr())
            .Then(cond => Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl)
                          .IgnoreThen(GetBlock())
                          .Then(thenBlock =>
                          ElseIf.Many()
                          .Then(elseIfs => Superpower.Parsers.Token.Sequence(Token.KwElse, Token.Colon, Token.Nl)
                                              .IgnoreThen(GetBlock())
                                              .OptionalOrDefault(Array.Empty<Stmt>())
                                              .Select(elseBlock => (Stmt)IfStmt.Build((cond, thenBlock), elseIfs, elseBlock)))));

        private static readonly TokenListParser<Token, Stmt> ParseWhileStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwWhile)
            .IgnoreThen(GetExpr())
            .Then(cond => Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl)
                          .IgnoreThen(GetBlock())
                          .Select(block => (Stmt)new WhileStmt(cond, block)));

        private static readonly TokenListParser<Token, MatchStmt.Patt> ParsePatt =
            Superpower.Parsers.Token.EqualTo(Token.Sym).Select(sym =>
            {
                if (sym.ToStringValue() == "_") return new MatchStmt.AnyPatt();
                else return (MatchStmt.Patt)new MatchStmt.NamePatt(sym.ToStringValue());
            }).Or(Superpower.Parsers.Token.EqualTo(Token.Int)
                .Select(n => (MatchStmt.Patt)new MatchStmt.IntLitPatt(int.Parse(n.ToStringValue()))));

        private static readonly TokenListParser<Token, (MatchStmt.Patt, Stmt[])> ParseMatchCase =
            ParsePatt.Then(patt =>
                Superpower.Parsers.Token.EqualTo(Token.Arrow)
                .IgnoreThen(GetStmt())
                .Then(headStmt =>
                    GetBlock().OptionalOrDefault(Array.Empty<Stmt>())
                    .Select(tailStmts => (patt, tailStmts.Prepend(headStmt).ToArray()))));

        private static readonly TokenListParser<Token, Stmt> ParseMatchStmt =
            Superpower.Parsers.Token.EqualTo(Token.KwMatch)
            .IgnoreThen(GetExpr())
            .Then(scr => Superpower.Parsers.Token.Sequence(Token.Colon, Token.Nl)
                         .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Indent))
                         .IgnoreThen(ParseMatchCase.AtLeastOnce())
                         .Then(cases => Superpower.Parsers.Token.EqualTo(Token.Dedent)
                                        .Value((Stmt)new MatchStmt(scr, cases))));

        private static readonly TokenListParser<Token, Stmt> ParseStmt =
            LetStmt.Or(ReturnStmt).Or(ParseIfStmt).Or(ParseWhileStmt).Or(ParseMatchStmt).Or(AssignOrDiscardStmt);

        private static TokenListParser<Token, Stmt> GetStmt() { return ParseStmt; }

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

        private static readonly TokenListParser<Token, Func> ParseExternFuncDef =
            Superpower.Parsers.Token.Sequence(
                Token.KwExtern, Token.KwDef, Token.Sym, Token.LParen)
            .Then((x) => FuncDefArgs.Then(funcArgs => Superpower.Parsers.Token.EqualTo(Token.RParen)
                    .IgnoreThen(RetType)
                    .Then(retType => Superpower.Parsers.Token.EqualTo(Token.Nl)
                    .Value(new Func(x[2].ToStringValue(), funcArgs, Array.Empty<Stmt>(), retType, true)))));


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
                .Then(fields => ParseFuncDef.Many()
                                .Then(methods => 
                                    Superpower.Parsers.Token.EqualTo(Token.Dedent)
                                    .Value(new ClassDef(className.ToStringValue(), fields, methods)))));

        private static readonly TokenListParser<Token, string> ParseInclude =
             Superpower.Parsers.Token.EqualTo(Token.KwInclude)
            .IgnoreThen(Superpower.Parsers.Token.EqualTo(Token.Sym))
            .Then(fileName => Superpower.Parsers.Token.EqualTo(Token.Nl).AtLeastOnce().Value(fileName.ToStringValue()));

        private static readonly TokenListParser<Token, (string[], ast.Program)> ParseProgram =
            Superpower.Parsers.Token.EqualTo(Token.Nl).Many()
            .IgnoreThen(ParseInclude.Many())
            .Then(includes =>
                ParseClassDef.Select(x => (object)x).Or(ParseFuncDef.Or(ParseExternFuncDef).Select(x => (object)x))
                .Then(x => Superpower.Parsers.Token.EqualTo(Token.Nl).Many().Value(x))
                .Many()
                .Select(xs => 
                {
                    var classes = xs.Where(x => x is ClassDef).Select(x => (ClassDef)x).ToArray();
                    var funcs = xs.Where(x => x is Func).Select(x => (Func)x).ToArray();
                    return (includes, new ast.Program(classes, funcs));
                }))
            .Then(x => Superpower.Parsers.Token.EqualTo(Token.Eoi).Value(x));

        public (string[], ast.Program) LexAndParse(string s)
        {
            instance = this;
            var tokens = Lexer.TokenizeString(s);
            var result = ParseProgram(new Superpower.Model.TokenList<Token>(tokens.ToArray()));
            if (!result.HasValue)
                throw new CompilerError(result.ToString(), new SourcePos(result.ErrorPosition.Line, result.ErrorPosition.Column));
            instance = null;
            return result.Value;
        }

        public static TokenListParser<TKind, T> Lazy<TKind, T>(Func<TokenListParser<TKind, T>> parser)
        {
            return (tokens) => parser()(tokens);
        }
        
    }
}
