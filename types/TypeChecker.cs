using RoisLang.ast;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class TypeChecker
    {
        private ScopedDictionary<string, TypeRef> Symbols;
        private TypeRef? Ret;

        public TypeChecker()
        {
            Symbols = new ScopedDictionary<string, TypeRef>();
            Ret = null;
        }

        public void TypeckProgram(ast.Program program)
        {
            Symbols.Reset();
            foreach (var func in program.Functions)
            {
                var ftype = FuncType.New(func.Arguments.Select(x => x.Item2).ToList(), func.Ret);
                if (Symbols.Contains(func.Name)) throw new Exception();
                Symbols.Add(func.Name, ftype);
            }
            // type-check all functions
            foreach (var func in program.Functions)
                TypeckFunc(func);
        }

        private void TypeckFunc(Func f)
        {
            using var _ = Symbols.EnterNewScope();
            Ret = f.Ret;
            foreach (var (argName, argTy) in f.Arguments)
            {
                Symbols.Add(argName, argTy);
            }
            foreach (var stmt in f.Body)
            {
                TypeckStmt(stmt);
            }
            if (!Ret.IsVoid && f.Body.Last() is not ReturnStmt)
                throw new Exception("Non-void functions must end with a return statement");
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
                        Symbols.Add(letAssignStmt.VarName, value);
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
                case ast.ReturnStmt returnStmt:
                    {
                        var typ = TypeckExpr(returnStmt.Value);
                        if (!typ.Equal(Ret!))
                            throw new Exception("Typechecking error");
                        return;
                    }
                case IfStmt ifStmt:
                    {
                        var cond = TypeckExpr(ifStmt.Cond);
                        if (!cond.IsBool) throw new Exception("Typechecking error");
                        // the if-statement body is a new scope
                        using (var _ = Symbols.EnterNewScope())
                        {
                            foreach (var thenStmt in ifStmt.Then)
                                TypeckStmt(thenStmt);
                        }
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
                case ast.BoolLit:
                    expr.Ty = TypeRef.BOOL;
                    break;
                case ast.VarExpr varExpr:
                    if (Symbols.Contains(varExpr.Name))
                        expr.Ty = Symbols[varExpr.Name];
                    else throw new Exception();
                    break;
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = TypeckExpr(binOpExpr.Lhs);
                        var rhs = TypeckExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op is BinOpExpr.Ops.Add or BinOpExpr.Ops.Sub or BinOpExpr.Ops.Mul)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw new Exception("Typechecking error");
                            expr.Ty = TypeRef.INT;
                        } else if (binOpExpr.Op is BinOpExpr.Ops.CmpEq or BinOpExpr.Ops.CmpNe or
                                   BinOpExpr.Ops.CmpLt or BinOpExpr.Ops.CmpLe or
                                   BinOpExpr.Ops.CmpGt or BinOpExpr.Ops.CmpGe)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw new Exception("Typechecking error");
                            expr.Ty = TypeRef.BOOL;
                        }
                        break;
                    }
                case CallExpr callExpr:
                    {
                        var callee = TypeckExpr(callExpr.Callee);
                        if (callee is FuncType ftype)
                        {
                            if (ftype.Args.Count != callExpr.Args.Length) throw new Exception("Typechecking error");
                            for (int i = 0; i < ftype.Args.Count; i++)
                            {
                                if (!TypeckExpr(callExpr.Args[i]).Equal(ftype.Args[i]))
                                    throw new Exception("Typechecking error");
                            }
                            callExpr.Ty = ftype.Ret;
                        }
                        else throw new Exception("Typechecking error");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (expr.Ty is TypeUnknown)
                throw new Exception("Typechecking error");
            return expr.Ty;
        }
    }
}
