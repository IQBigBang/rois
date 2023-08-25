using RoisLang.ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class TypeChecker
    {
        private Dictionary<string, TypeRef> Locals;

        public TypeChecker()
        {
            Locals = new Dictionary<string, TypeRef>();
        }

        public void TypeckFunc(Func f)
        {
            Locals.Clear();
            foreach (var argName in f.Arguments)
            {
                Locals.Add(argName, TypeRef.INT); // TODO
            }
            foreach (var stmt in f.Body)
            {
                TypeckStmt(stmt);
            }
        }

        void TypeckStmt(Stmt stmt)
        {
            switch (stmt)
            {
                case ast.DiscardStmt discardStmt:
                    TypeckExpr(discardStmt.Expr);
                    return;
                case ast.LetAssignStmt letAssignStmt:
                    {
                        var value = TypeckExpr(letAssignStmt.Value);
                        Locals[letAssignStmt.VarName] = value;
                        return;
                    }
                case ast.AssignStmt assignStmt:
                    {
                        var valueType = TypeckExpr(assignStmt.Value);
                        var lhs = TypeckExpr(assignStmt.Lhs);
                        if (!valueType.Equal(lhs))
                            throw new Exception("Typechecking error");
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        TypeRef TypeckExpr(Expr expr)
        {
            switch (expr)
            {
                case ast.IntExpr:
                    expr.Ty = TypeRef.INT;
                    break;
                case ast.VarExpr varExpr:
                    expr.Ty = Locals[varExpr.Name];
                    break;
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = TypeckExpr(binOpExpr.Lhs);
                        var rhs = TypeckExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op == BinOpExpr.Ops.Add)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw new Exception("Typechecking error");
                            expr.Ty = TypeRef.INT;
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
            if (expr.Ty is TypeUnknown)
                throw new Exception("Typechecking error");
            return expr.Ty;
        }
    }
}
