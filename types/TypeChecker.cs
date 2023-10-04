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
        private ClassDef? StrClass;
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
            StrClass = program.Classes.FirstOrDefault(c => c.Name == "Str") ?? throw new CompilerError("No Str class defined");

            foreach (var func in program.Functions)
            {
                var ftype = FuncType.New(func.Arguments.Select(x => x.Item2).ToList(), func.Ret);
                if (Symbols.Contains(func.Name)) throw new CompilerError($"Double definition of function `{func.Name}`");
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
                    throw new CompilerError($"Double definition of methods in class {cls.Name}");
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
                throw new CompilerError($"Non-void functions must end with a return statement (function `{f.Name}`)");
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
                            throw new CompilerError("Typechecking error: assignment statement");
                        return;
                    }
                case ast.ReturnStmt returnStmt:
                    {
                        var typ = TypeckExpr(returnStmt.Value);
                        if (!typ.Equal(Ret!))
                            throw new CompilerError("Typechecking error: return statement");
                        return;
                    }
                case IfStmt ifStmt:
                    {
                        var cond = TypeckExpr(ifStmt.Cond);
                        if (!cond.IsBool) throw new CompilerError("Typechecking error: if condition");
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
                        if (!cond.IsBool) throw new CompilerError("Typechecking error: while condition");
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
                case ast.StrLit:
                    expr.Ty = StrClass!.Type!;
                    break;
                case ast.VarExpr varExpr:
                    if (Symbols.Contains(varExpr.Name))
                        expr.Ty = Symbols[varExpr.Name];
                    else throw new CompilerError($"Undefined variable `{varExpr.Name}`");
                    break;
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = TypeckExpr(binOpExpr.Lhs);
                        var rhs = TypeckExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op is BinOpExpr.Ops.Add or BinOpExpr.Ops.Sub or BinOpExpr.Ops.Mul)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw new CompilerError("Typechecking error: binary operation");
                            expr.Ty = TypeRef.INT;
                        } else if (binOpExpr.Op is BinOpExpr.Ops.CmpEq or BinOpExpr.Ops.CmpNe or
                                   BinOpExpr.Ops.CmpLt or BinOpExpr.Ops.CmpLe or
                                   BinOpExpr.Ops.CmpGt or BinOpExpr.Ops.CmpGe)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw new CompilerError("Typechecking error: binary operation");
                            expr.Ty = TypeRef.BOOL;
                        }
                        break;
                    }
                case CallExpr callExpr:
                    {
                        var callee = TypeckExpr(callExpr.Callee);
                        if (callee is FuncType ftype)
                        {
                            if (ftype.Args.Count != callExpr.Args.Length) throw new CompilerError("Typechecking error: wrong number of arguments");
                            for (int i = 0; i < ftype.Args.Count; i++)
                            {
                                if (!TypeckExpr(callExpr.Args[i]).Equal(ftype.Args[i]))
                                    throw new CompilerError("Typechecking error: wrong argument type");
                            }
                            callExpr.Ty = ftype.Ret;
                        }
                        else throw new CompilerError("Typechecking error: non-function types cannot be called");
                    }
                    break;
                case MemberExpr memberExpr:
                    {
                        var objectType = TypeckExpr(memberExpr.Object);
                        if (!objectType.IsClass)
                            throw new CompilerError("Typechecking error: non-object types don't have members");
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
                            throw new CompilerError("All fields must be initialized in a constructor");
                        for (int i = 0; i < constrExpr.Fields.Count; ++i)
                        {
                            var (fieldName, fieldType) = constrExpr.Class.Fields[i];
                            var fieldExpr = constrExpr.Fields.GetValueOrDefault(fieldName) ?? throw new CompilerError("All fields must be initialized in a constructor");
                            if (!TypeckExpr(fieldExpr).Equal(fieldType))
                                throw new CompilerError("Typechecking error: invalid type of field");
                        }
                        constrExpr.Ty = constrExpr.Class;
                    }
                    break;
                case MethodCallExpr mCallExpr:
                    {
                        var obj = TypeckExpr(mCallExpr.Object);
                        if (!obj.IsClass)
                            throw new CompilerError("Typechecking error: non-object types don't have methods"); // TODO ?
                        var classDef = ClassesTable[(ClassType)obj];
                        Func? method = null;
                        foreach (var m in classDef.Methods)
                            if (m.Name == mCallExpr.methodName) method = m;
                        if (method == null) throw new CompilerError("Undefined method");
                        // Now the ordinary Call check
                        if (method.Arguments.Length != mCallExpr.Args.Length) 
                            throw new CompilerError("Typechecking error: wrong number of arguments");
                        for (int i = 0; i < method.Arguments.Length; i++)
                        {
                            if (!TypeckExpr(mCallExpr.Args[i]).Equal(method.Arguments[i].Item2))
                                throw new CompilerError("Typechecking error: wrong argument type");
                        }
                        mCallExpr.Ty = method.Ret;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (expr.Ty is TypeUnknown)
                throw new Exception("Internal typechecking error");
            return expr.Ty;
        }
    }
}
