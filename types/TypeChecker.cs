using RoisLang.ast;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class TypeChecker
    {
        private ScopedDictionary<string, TypeRef> Symbols;
        // this is for looking up methods
        private Dictionary<ClassType, ClassDef> ClassesTable;
        private TypeRef? Ret;

        public TypeChecker()
        {
            Symbols = new ScopedDictionary<string, TypeRef>();
            ClassesTable = new Dictionary<ClassType, ClassDef>();
            Ret = null;
        }

        public void TypeckProgram(ast.Program program)
        {
            Symbols.Reset();
            foreach (var func in program.Functions)
            {
                var ftype = FuncType.New(func.Arguments.Select(x => x.Item2).ToList(), func.Ret);
                if (Symbols.Contains(func.Name)) throw new Exception();
                Symbols.AddNew(func.Name, ftype);
            }
            foreach (var cls in program.Classes)
                ClassesTable.Add(cls.Type!, cls);
            // type-check all functions
            foreach (var func in program.Functions)
                TypeckFunc(func);
            // type-check all methods
            foreach (var cls in program.Classes)
            {
                // also make sure there are no duplicite names
                if (cls.Methods.DistinctBy(cls => cls.Name).Count() != cls.Methods.Length)
                    throw new Exception("Double definition of method");
                foreach (var method in cls.Methods)
                    TypeckFunc(method, cls);
            }
        }

        private void TypeckExternFunc(Func f)
        {
            // void arguments in extern functions don't really make sense
            Debug.Assert(!f.Arguments.Any(x => x.Item2.IsVoid));
            // TODO: more type-checks once more complex types are supported
        }

        private void TypeckFunc(Func f, ClassDef? self = null)
        {
            if (f.Extern) { TypeckExternFunc(f); return; }
            using var _ = Symbols.EnterNewScope();
            if (self != null) Symbols.AddNew("self", self.Type!);
            Ret = f.Ret;
            foreach (var (argName, argTy) in f.Arguments)
            {
                Symbols.AddNew(argName, argTy);
            }
            foreach (var stmt in f.Body)
            {
                TypeckStmt(stmt);
            }
            if (!Ret.IsVoid && !CheckBlockReturns(f.Body))
                throw new Exception("Non-void functions must end with a return statement");
        }

        private bool CheckBlockReturns(Stmt[] block)
        {
            if (block.Length == 0) return false;
            if (block.Last() is ReturnStmt) return true;
            if (block.Last() is IfStmt ifStmt)
                return CheckBlockReturns(ifStmt.Then) && CheckBlockReturns(ifStmt.Else);
            return false;
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
                        Symbols.AddNew(letAssignStmt.VarName, value);
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
                            foreach (var thenStmt in ifStmt.Then) TypeckStmt(thenStmt);
                        }
                        using (var _ = Symbols.EnterNewScope())
                        {
                            foreach (var elseStmt in ifStmt.Else) TypeckStmt(elseStmt);
                        }
                        return;
                    }
                case WhileStmt whileStmt:
                    {
                        var cond = TypeckExpr(whileStmt.Cond);
                        if (!cond.IsBool) throw new Exception("Typechecking error");
                        using (var _ = Symbols.EnterNewScope())
                        {
                            foreach (var stmt_ in whileStmt.Body) TypeckStmt(stmt_);
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
                case MemberExpr memberExpr:
                    {
                        var objectType = TypeckExpr(memberExpr.Object);
                        if (!objectType.IsClass)
                            throw new Exception("Typechecking error");
                        foreach (var field in ((ClassType)objectType).Fields)
                        {
                            if (field.Item1 == memberExpr.MemberName)
                            {
                                memberExpr.Ty = field.Item2;
                                break;
                            }
                        }
                    }
                    break;
                case ConstructorExpr constrExpr:
                    {
                        if (constrExpr.Fields.Count != constrExpr.Class.Fields.Length)
                            throw new Exception("All fields must be initialized");
                        for (int i = 0; i < constrExpr.Fields.Count; ++i)
                        {
                            var (fieldName, fieldType) = constrExpr.Class.Fields[i];
                            var fieldExpr = constrExpr.Fields.GetValueOrDefault(fieldName) ?? throw new Exception("Uninitialized fields");
                            if (!TypeckExpr(fieldExpr).Equal(fieldType))
                                throw new Exception("Invalid type of field");
                        }
                        constrExpr.Ty = constrExpr.Class;
                    }
                    break;
                case MethodCallExpr mCallExpr:
                    {
                        var obj = TypeckExpr(mCallExpr.Object);
                        if (!obj.IsClass)
                            throw new Exception("Typechecking error");
                        var classDef = ClassesTable[(ClassType)obj];
                        Func? method = null;
                        foreach (var m in classDef.Methods)
                            if (m.Name == mCallExpr.methodName) method = m;
                        if (method == null) throw new Exception("Undefined method");
                        // Now the ordinary Call check
                        if (method.Arguments.Length != mCallExpr.Args.Length) throw new Exception("Typechecking error");
                        for (int i = 0; i < method.Arguments.Length; i++)
                        {
                            if (!TypeckExpr(mCallExpr.Args[i]).Equal(method.Arguments[i].Item2))
                                throw new Exception("Typechecking error");
                        }
                        mCallExpr.Ty = method.Ret;
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
