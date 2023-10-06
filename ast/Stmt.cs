using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.ast
{
    public abstract record Stmt();
    public record DiscardStmt(Expr Expr) : Stmt();
    public record LetAssignStmt(string VarName, Expr Value) : Stmt();
    public record AssignStmt(Expr Lhs, Expr Value) : Stmt();
    public record ReturnStmt(Expr Value) : Stmt();
    public record IfStmt : Stmt
    {
        public IfStmt(Expr cond, Stmt[] then, Stmt[] @else) : base()
        {
            Cond = cond;
            Then = then;
            Else = @else;
        }

        public Expr Cond { get; init; }
        public Stmt[] Then { get; set; }
        public Stmt[] Else { get; set; }

        public bool HasElse => Else.Length > 0;
        public static IfStmt Build((Expr, Stmt[]) If, (Expr, Stmt[])[] ElseIfs, Stmt[] Else)
        {
            // build from the end up
            Stmt[] tree = Else;
            for (int i = ElseIfs.Length - 1; i >= 0; i--)
            {
                var stmt = new IfStmt(ElseIfs[i].Item1, ElseIfs[i].Item2, tree);
                tree = new Stmt[] { stmt };
            }
            return new IfStmt(If.Item1, If.Item2, tree);
        }
        public static Stmt Build(List<(Expr, Stmt[])> ElseIfs, Stmt Else)
        {
            Stmt tree = Else;
            for (int i = ElseIfs.Count - 1; i >= 0; i--)
            {
                var stmt = new IfStmt(ElseIfs[i].Item1, ElseIfs[i].Item2, new Stmt[] { tree });
                tree = stmt;
            }
            return tree;
        }
    }
    public record WhileStmt : Stmt
    {
        public Expr Cond { get; init; }
        public Stmt[] Body { get; set; }

        public WhileStmt(Expr cond, Stmt[] body) : base()
        {
            Cond = cond;
            Body = body;
        }
    }
    public record MatchStmt(Expr Scrutinee, (MatchStmt.Patt, Stmt[])[] Cases) : Stmt()
    {
        public abstract record Patt(SourcePos Pos);
        // '_'
        public record AnyPatt(SourcePos Pos) : Patt(Pos);
        public record IntLitPatt(int Val, SourcePos Pos) : Patt(Pos);
        public record NamePatt(string Name, SourcePos Pos) : Patt(Pos);
    }
}
