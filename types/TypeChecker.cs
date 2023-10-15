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
        private Dictionary<NamedType, ClassDef> ClassesTable;
        private ast.Program? CurrProgram;
        private ClassDef? StrClass;
        private TypeRef? Ret;

        public TypeChecker()
        {
            Symbols = new ScopedDictionary<string, TypeRef>();
            ClassesTable = new Dictionary<NamedType, ClassDef>();
            Ret = null;
        }

        public void TypeckProgram(ast.Program program)
        {
            Symbols.Reset();
            CurrProgram = program;
            StrClass = program.Classes.FirstOrDefault(c => c.Name == "Str") ?? throw CompilerError.NameErr("No Str class defined", SourcePos.Zero);

            foreach (var func in program.Functions)
            {
                var ftype = FuncType.New(func.Arguments.Select(x => x.Item2).ToList(), func.Ret);
                if (Symbols.Contains(func.Name)) throw CompilerError.NameErr($"Double definition of function `{func.Name}`", func.Pos);
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
                var uniqueNames = new HashSet<string>();
                foreach (var method in cls.Methods)
                    if (uniqueNames.Contains(method.Name))
                        throw CompilerError.TypeErr($"Double definition of method", method.Pos);
                    else
                        uniqueNames.Add(method.Name);
                uniqueNames.Clear();
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
                throw CompilerError.ValidationErr($"Non-void functions must end with a return statement", f.Pos);
        }

        private bool CheckBlockReturns(Stmt[] block)
        {
            if (block.Length == 0) return false;
            if (block.Last() is ReturnStmt) return true;
            if (block.Last() is IfStmt ifStmt)
                return CheckBlockReturns(ifStmt.Then) && CheckBlockReturns(ifStmt.Else);
            if (block.Last() is MatchStmt matchStmt)
                return matchStmt.Cases.All(x => CheckBlockReturns(x.Item2));
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
                            throw CompilerError.TypeErr("", assignStmt.Lhs.Pos);
                        return;
                    }
                case ast.ReturnStmt returnStmt:
                    {
                        var typ = TypeckExpr(returnStmt.Value);
                        if (!typ.Equal(Ret!))
                            throw CompilerError.TypeErr("Wrong return type", returnStmt.Value.Pos);
                        return;
                    }
                case IfStmt ifStmt:
                    {
                        var cond = TypeckExpr(ifStmt.Cond);
                        if (!cond.IsBool) throw CompilerError.TypeErr("Condition must be a boolean", ifStmt.Cond.Pos);
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
                        if (!cond.IsBool) throw CompilerError.TypeErr("Condition must be a boolean", whileStmt.Cond.Pos);
                        using (var _ = Symbols.EnterNewScope())
                        {
                            foreach (var stmt_ in whileStmt.Body) TypeckStmt(stmt_);
                        }
                        return;
                    }
                case MatchStmt matchStmt:
                    {
                        var scrType = TypeckExpr(matchStmt.Scrutinee);
                        foreach (var (patt, body) in matchStmt.Cases)
                        {
                            using (var _ = Symbols.EnterNewScope())
                            {
                                TypeckPatt(patt, scrType);
                                foreach (var stmt_ in body) TypeckStmt(stmt_);
                            }
                        }
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private void TypeckPatt(MatchStmt.Patt patt, TypeRef expectedType)
        {
            switch (patt)
            {
                case MatchStmt.AnyPatt:
                    return;
                case MatchStmt.NamePatt namePatt: // equivalent to a `let`
                    Symbols.AddNew(namePatt.Name, expectedType);
                    return;
                case MatchStmt.IntLitPatt intPatt:
                    if (!expectedType.IsInt) throw CompilerError.TypeErr("Int pattern expects integer value", patt.Pos);
                    return;
                case MatchStmt.ObjectPatt objectPatt:
                    {
                        // find the appropriate class
                        ClassDef cls = CurrProgram!.Classes.FirstOrDefault(cls => cls.Name == objectPatt.ObjName) ??
                                            throw CompilerError.NameErr("Class couldn't be found", objectPatt.Pos);
                        if (!cls.Type!.Equal(expectedType))
                            throw CompilerError.TypeErr("Pattern doesn't match the scrutinee type", objectPatt.Pos);
                        if (cls.Fields.Length != objectPatt.Members.Length)
                            throw CompilerError.TypeErr("Object pattern has more or less members than the type", objectPatt.Pos);
                        for (int i = 0; i < cls.Fields.Length; i++)
                        {
                            TypeckPatt(objectPatt.Members[i], cls.Fields[i].Item1);
                        }
                        objectPatt.ClsType = cls.Type!;
                    }
                    return;
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
                case ast.CharLit:
                    expr.Ty = TypeRef.CHAR;
                    break;
                case ast.StrLit:
                    expr.Ty = StrClass!.Type!;
                    break;
                case ast.VarExpr varExpr:
                    if (Symbols.Contains(varExpr.Name))
                        expr.Ty = Symbols[varExpr.Name];
                    else throw CompilerError.NameErr($"Undefined variable `{varExpr.Name}`", varExpr.Pos);
                    break;
                case UnOpExpr unOpExpr:
                    {
                        var subExpr = TypeckExpr(unOpExpr.Exp);
                        if (unOpExpr.Op == UnOpExpr.Ops.Not)
                        {
                            if (!subExpr.IsBool) throw CompilerError.TypeErr("Operand must be bool", unOpExpr.Pos);
                            unOpExpr.Ty = TypeRef.BOOL;
                        }
                        else if (unOpExpr.Op == UnOpExpr.Ops.Neg)
                        {
                            if (!subExpr.IsInt) throw CompilerError.TypeErr("Operand must be int", unOpExpr.Pos);
                            unOpExpr.Ty = TypeRef.INT;
                        }
                        else throw new NotImplementedException();
                        break;
                    }
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = TypeckExpr(binOpExpr.Lhs);
                        var rhs = TypeckExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op is BinOpExpr.Ops.Add or BinOpExpr.Ops.Sub or BinOpExpr.Ops.Mul)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw CompilerError.TypeErr("Both operands must be int", binOpExpr.Pos);
                            expr.Ty = TypeRef.INT;
                        } else if (binOpExpr.Op is BinOpExpr.Ops.CmpEq or BinOpExpr.Ops.CmpNe or
                                   BinOpExpr.Ops.CmpLt or BinOpExpr.Ops.CmpLe or
                                   BinOpExpr.Ops.CmpGt or BinOpExpr.Ops.CmpGe)
                        {
                            if (!lhs.IsInt || !rhs.IsInt)
                                throw CompilerError.TypeErr("Both operands must be int", binOpExpr.Pos);
                            expr.Ty = TypeRef.BOOL;
                        } else if (binOpExpr.Op is BinOpExpr.Ops.And or BinOpExpr.Ops.Or)
                        {
                            if (!lhs.IsBool || !rhs.IsBool)
                                throw CompilerError.TypeErr("Both operands must be boolean", binOpExpr.Pos);
                            expr.Ty = TypeRef.BOOL;
                        }
                        break;
                    }
                case CallExpr callExpr:
                    {
                        var callee = TypeckExpr(callExpr.Callee);
                        if (callee is FuncType ftype)
                        {
                            if (ftype.Args.Count != callExpr.Args.Length) 
                                throw CompilerError.TypeErr("Wrong number of arguments", callExpr.Pos);
                            for (int i = 0; i < ftype.Args.Count; i++)
                            {
                                if (!TypeckExpr(callExpr.Args[i]).Equal(ftype.Args[i]))
                                    throw CompilerError.TypeErr("Wrong argument type", callExpr.Args[i].Pos);
                            }
                            callExpr.Ty = ftype.Ret;
                        }
                        else throw CompilerError.TypeErr("Non-function type cannot be called", callExpr.Pos);
                    }
                    break;
                case MemberExpr memberExpr:
                    {
                        var objectType = TypeckExpr(memberExpr.Object);
                        if (!objectType.IsStructClass)
                            throw CompilerError.TypeErr("Non-object types don't have members", memberExpr.Object.Pos);
                        foreach (var field in ((NamedType)objectType).Fields)
                        {
                            if (field.Item2 == memberExpr.MemberName)
                            {
                                memberExpr.Ty = field.Item1;
                                break;
                            }
                        }
                        if (memberExpr.Ty is TypeUnknown)
                            throw CompilerError.NameErr("Undefined member", memberExpr.Pos);
                    }
                    break;
                case ConstructorExpr constrExpr:
                    {
                        if (constrExpr.Fields.Count != constrExpr.Class.Fields.Length)
                            throw CompilerError.ValidationErr("All fields must be initialized in a constructor", constrExpr.Pos);
                        for (int i = 0; i < constrExpr.Fields.Count; ++i)
                        {
                            var (fieldType, fieldName) = constrExpr.Class.Fields[i];
                            var fieldExpr = constrExpr.Fields.GetValueOrDefault(fieldName) 
                                ?? throw CompilerError.ValidationErr("All fields must be initialized in a constructor", constrExpr.Pos);
                            if (!TypeckExpr(fieldExpr).Equal(fieldType))
                                throw CompilerError.TypeErr("Wrong type of field", fieldExpr.Pos);
                        }
                        constrExpr.Ty = constrExpr.Class;
                    }
                    break;
                case MethodCallExpr mCallExpr:
                    {
                        var obj = TypeckExpr(mCallExpr.Object);
                        if (!obj.IsStructClass) // TODO: enum class methods
                            throw CompilerError.TypeErr("Non-object types don't have methods", mCallExpr.Object.Pos);  // TODO ?
                        var classDef = ClassesTable[(NamedType)obj];
                        Func? method = null;
                        foreach (var m in classDef.Methods)
                            if (m.Name == mCallExpr.methodName) method = m;
                        if (method == null) throw CompilerError.NameErr("Undefined method", mCallExpr.Pos);
                        // Now the ordinary Call check
                        if (method.Arguments.Length != mCallExpr.Args.Length)
                            throw CompilerError.TypeErr("Wrong number of arguments", mCallExpr.Pos);
                        for (int i = 0; i < method.Arguments.Length; i++)
                        {
                            if (!TypeckExpr(mCallExpr.Args[i]).Equal(method.Arguments[i].Item2))
                                throw CompilerError.TypeErr("Wrong argument type", mCallExpr.Args[i].Pos);
                        }
                        mCallExpr.Ty = method.Ret;
                    }
                    break;
                case CastAsExpr castExpr:
                    {
                        // castAs provides cheap and safe conversions
                        // int <- bool
                        // char <-> int
                        var valueType = TypeckExpr(castExpr.Value);
                        if (valueType.IsInt && castExpr.CastType is CharType)
                            castExpr.Ty = castExpr.CastType;
                        else if (valueType is CharType && castExpr.CastType is IntType)
                            castExpr.Ty = castExpr.CastType;
                        else if (valueType.IsBool && castExpr.CastType is IntType)
                            castExpr.Ty = castExpr.CastType;
                        else
                            throw CompilerError.ValidationErr($"Invalid cast from {valueType} to {castExpr.CastType}", castExpr.Pos);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (expr.Ty is TypeUnknown)
                throw CompilerError.ValidationErr("Couldn't infer type", expr.Pos);
            return expr.Ty;
        }
    }
}
