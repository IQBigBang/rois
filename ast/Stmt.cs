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
}
