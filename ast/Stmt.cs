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
    public record IfStmt(Expr Cond, Stmt[] Then, Stmt[] Else) : Stmt()
    {
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
    }
    public record WhileStmt(Expr Cond, Stmt[] Body) : Stmt();
}
