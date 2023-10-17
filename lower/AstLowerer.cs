using RoisLang.ast;
using RoisLang.mid_ir;
using RoisLang.mid_ir.builder;
using RoisLang.types;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.lower
{
    public class AstLowerer
    {
        private MidFunc? currentFunc;
        private MidBuilder Builder;
        private ScopedDictionary<string, MidValue> Symbols;
        private Dictionary<ValueTuple<string, string>, MidValue> Methods;
        private NamedType? StrType;

        public AstLowerer()
        {
            Builder = new MidBuilder();
            Symbols = new ScopedDictionary<string, MidValue>();
            Methods = new Dictionary<(string, string), MidValue>();
        }

        public MidModule LowerProgram(ast.Program program)
        {
            Symbols.Reset();
            List<MidFunc> midFuncs = new();
            foreach (var func in program.Functions)
            {
                if (Symbols.Contains(func.Name)) throw CompilerError.NameErr($"Function `{func.Name}` defined twice", func.Pos);
                var midFunc = new MidFunc(func.Name, func.Arguments.Select(x => x.Item2).ToList(), func.Ret, null, func.Extern);
                midFuncs.Add(midFunc);
                var value = MidValue.Global(midFunc, Assertion.X);
                Symbols.AddNew(func.Name, value);
            }
            StrType = program.UserTypes.First(c => c.Name == "Str").Type;
            foreach (var cls in program.UserTypes)
            {
                foreach (var method in cls.Methods)
                {
                    var args = new List<TypeRef> { cls.Type! }.Concat(method.Arguments.Select(x => x.Item2)).ToList();
                    var midFunc = new MidFunc($"{cls.Name}${method.Name}", args, method.Ret, cls.Type);
                    var value = MidValue.Global(midFunc, Assertion.X);
                    Methods[(cls.Name, method.Name)] = value;
                }
            }
            for (int i = 0; i < program.Functions.Length; i++)
                LowerFunc(program.Functions[i], midFuncs[i]);
            foreach (var cls in program.UserTypes)
            {
                foreach (var method in cls.Methods)
                {
                    LowerFunc(method, Methods[(cls.Name, method.Name)].GetGlobalValue());
                } 
            }
            return new MidModule(Methods.Select(x => x.Value.GetGlobalValue()).Concat(midFuncs).ToList(), 
                program.UserTypes.Select(x => x.Type!).ToList());
        }

        private void LowerFunc(Func f, MidFunc target)
        {
            if (f.Extern) return;
            currentFunc = target;
            Builder.SwitchBlock(target.EntryBlock);
            using var _ = Symbols.EnterNewScope();
            if (target.IsMethod)
            {
                Symbols.AddNew("self", target.EntryBlock.Argument(0));
                for (int i = 0; i < f.Arguments.Length; i++)
                {
                    Symbols.AddNew(f.Arguments[i].Item1, target.EntryBlock.Argument(i + 1));
                }
            }
            else
            {
                // add arguments
                for (int i = 0; i < f.Arguments.Length; i++)
                {
                    Symbols.AddNew(f.Arguments[i].Item1, target.EntryBlock.Argument(i));
                }
            }
            // compile the body
            foreach (var stmt in f.Body)
            {
                LowerStmt(stmt);
            }
            if (f.Ret.IsVoid)
                Builder.BuildRet();
        }

        MidValue LowerExpr(ast.Expr expr)
        {
            switch (expr)
            {
                case ast.IntExpr intExpr:
                    return MidValue.ConstInt(intExpr.Value);
                case ast.BoolLit boolExpr:
                    return MidValue.ConstBool(boolExpr.Value);
                case CharLit charExpr:
                    return MidValue.ConstChar((uint)charExpr.Ch);
                case StrLit strExpr:
                    {
                        // create the raw string
                        var rawStr = Builder.BuildConstString(strExpr.Text);
                        // allocate the `Str` type
                        var instance = Builder.BuildAllocClass(StrType!);
                        Builder.BuildStore(new FieldInfo(StrType!, 0), instance, rawStr);
                        return instance;
                    }
                case ast.VarExpr varExpr:
                    if (Symbols.Contains(varExpr.Name))
                        return Symbols[varExpr.Name];
                    else throw CompilerError.NameErr($"Undefined symbol `{varExpr.Name}` used", varExpr.Pos);
                case UnOpExpr unOpExpr:
                    {
                        var subExpr = LowerExpr(unOpExpr.Exp);
                        if (unOpExpr.Op == UnOpExpr.Ops.Not)
                            return Builder.BuildNot(subExpr);
                        if (unOpExpr.Op == UnOpExpr.Ops.Neg)
                            return Builder.BuildINeg(subExpr);
                        throw new NotImplementedException();
                    }
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = LowerExpr(binOpExpr.Lhs);
                        var rhs = LowerExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op is BinOpExpr.Ops.Add)
                            return Builder.BuildIAdd(lhs, rhs);
                        else if (binOpExpr.Op is BinOpExpr.Ops.Sub)
                            return Builder.BuildISub(lhs, rhs);
                        else if (binOpExpr.Op is BinOpExpr.Ops.Mul)
                            return Builder.BuildIMul(lhs, rhs);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpEq)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.Eq);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpNe)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.NEq);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpLt)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.Lt);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpLe)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.Le);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpGt)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.Gt);
                        else if (binOpExpr.Op is BinOpExpr.Ops.CmpGe)
                            return Builder.BuildICmp(lhs, rhs, MidICmpInstr.CmpOp.Ge);
                        else if (binOpExpr.Op is BinOpExpr.Ops.And)
                            return Builder.BuildAnd(lhs, rhs);
                        else if (binOpExpr.Op is BinOpExpr.Ops.Or)
                            return Builder.BuildOr(lhs, rhs);
                        else throw new NotImplementedException();
                    }
                case ast.CallExpr callExpr:
                    {
                        var callee = LowerExpr(callExpr.Callee);
                        var arguments = callExpr.Args.Select(x => LowerExpr(x)).ToArray();
                        return Builder.BuildCall(callee, arguments);
                    }
                case ast.MemberExpr memberExpr:
                    {
                        var obj = LowerExpr(memberExpr.Object);
                        return Builder.BuildLoad(obj, memberExpr.MemberName);
                    }
                case ConstructorExpr constrExpr:
                    {
                        if (constrExpr.ClassType!.IsStructClass)
                        {
                            var instance = Builder.BuildAllocClass(constrExpr.ClassType);
                            foreach (var (fieldName, fieldExpr) in constrExpr.Fields)
                            {
                                var fieldValue = LowerExpr(fieldExpr);
                                Builder.BuildStore(instance, fieldValue, fieldName);
                            }
                            return instance;
                        }
                        else if (constrExpr.ClassType!.IsEnumClass)
                        {
                            var instance = Builder.BuildAllocClass(constrExpr.ClassType);
                            int variantTag = Array.FindIndex(constrExpr.ClassType.Variants, x => x.VariantName == constrExpr.ConstrName);
                            Builder.BuildSetTag(instance, variantTag);
                            foreach (var (fieldName, fieldExpr) in constrExpr.Fields)
                            {
                                var fieldValue = LowerExpr(fieldExpr);
                                Builder.BuildStore(new FieldInfo(constrExpr.ClassType, variantTag, fieldName),
                                                   instance, fieldValue);
                            }
                            return instance;
                        }
                        else throw new Exception();
                    }
                case MethodCallExpr mCallExpr:
                    {
                        var obj = LowerExpr(mCallExpr.Object);
                        var cls = (NamedType)obj.GetType();
                        var method = Methods[(cls.Name, mCallExpr.methodName)];
                        var arguments = new List<MidValue> { obj }.Concat(mCallExpr.Args.Select(x => LowerExpr(x))).ToArray();
                        return Builder.BuildCall(method, arguments);
                    }
                case FailExpr failExpr:
                    {
                        Builder.BuildFail("Failure");
                        return MidValue.Null();
                    }
                case CastAsExpr castExpr:
                    {
                        var value = LowerExpr(castExpr.Value);
                        switch (castExpr.Value.Ty, castExpr.CastType)
                        {
                            // bool -> int   = bitcast
                            // char -> int   = bitcast
                            // int -> char   = bitcast (?)
                            case (BoolType, IntType):
                            case (CharType, IntType):
                            case (IntType, CharType):
                                return Builder.BuildBitcast(value, castExpr.CastType);
                            default:
                                throw new NotImplementedException();
                        }
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public void LowerStmt(ast.Stmt stmt)
        {
            switch (stmt)
            {
                case ast.DiscardStmt discardStmt:
                    LowerExpr(discardStmt.Expr);
                    return;
                case ast.LetAssignStmt letAssignStmt:
                    {
                        var value = LowerExpr(letAssignStmt.Value);
                        Symbols.AddNew(letAssignStmt.VarName, value);
                        return;
                    }
                case ast.AssignStmt assignStmt:
                    {
                        var value = LowerExpr(assignStmt.Value);
                        if (assignStmt.Lhs is ast.VarExpr varExpr)
                        {
                            Symbols.Set(varExpr.Name, value);
                        } else if (assignStmt.Lhs is MemberExpr memberExpr)
                        {
                            var obj = LowerExpr(memberExpr.Object);
                            Builder.BuildStore(obj, value, memberExpr.MemberName);
                        }
                        else throw new NotImplementedException();
                        return;
                    }
                case ast.ReturnStmt returnStmt:
                    {
                        var value = LowerExpr(returnStmt.Value);
                        Builder.BuildRet(value);
                        return;
                    }
                case ast.IfStmt ifStmt:
                    {
                        var cond = LowerExpr(ifStmt.Cond);
                        // collect all locals, because we have to pass them as arguments to the `if` and `else` blocks
                        var allLocals = GetAllLocals();
                        var typesList = allLocals.Select(x => x.Value.GetType()).ToList();
                        var ifBlock = currentFunc!.NewBlock(typesList);
                        var elseBlock = currentFunc.NewBlock(typesList);
                        var continueBlock = ifStmt.HasElse ? currentFunc.NewBlock(typesList) : elseBlock;
                        // take the values of `allLocals` and pass them to the blocks
                        var allLocalsValues = allLocals.Select(x => x.Value).ToArray();
                        var allLocalsNames = allLocals.Select(x => x.Key).ToList();
                        var branchInstr = Builder.BuildBranch(cond, ifBlock, allLocalsValues, elseBlock, allLocalsValues);
                        // now switch to the `if` block
                        // all the locals are now arguments to the ifBlock which means different `MidValue`s
                        // we call this a "context switch"
                        using (var _ = Symbols.EnterNewScope())
                        {
                            DoContextSwitch(allLocalsNames, ifBlock);
                            Builder.SwitchBlock(ifBlock);
                            // now compile the `if` body
                            foreach (var stmt1 in ifStmt.Then)
                            {
                                LowerStmt(stmt1);
                            }
                            // switch from `if` to `continue`
                            Builder.BuildGoto(continueBlock, allLocalsNames.Select(name => Symbols[name]).ToArray());
                        }
                        // if there is an `else` block, write it
                        if (ifStmt.HasElse)
                        {
                            using var _ = Symbols.EnterNewScope();
                            DoContextSwitch(allLocalsNames, elseBlock);
                            Builder.SwitchBlock(elseBlock);
                            foreach (var stmt1 in ifStmt.Else)
                                LowerStmt(stmt1);
                            // switch from `else` to `continue`
                            Builder.BuildGoto(continueBlock, allLocalsNames.Select(name => Symbols[name]).ToArray());
                        }
                        // now we switch to the `continue` block
                        // instead of establishing a new scope, we wipe out the current one
                        Symbols.ClearCurrentScope();
                        DoContextSwitch(allLocalsNames, continueBlock);
                        Builder.SwitchBlock(continueBlock);
                        // compilation of the rest can resume business-as-usual
                        return; 
                    }
                case WhileStmt whileStmt:
                    {
                        // collect all locals
                        var allLocals = GetAllLocals();
                        var typesList = allLocals.Select(x => x.Value.GetType()).ToList();
                        var bodyBlock = currentFunc!.NewBlock(typesList);
                        var continueBlock = currentFunc!.NewBlock(typesList);
                        // compile condition
                        var cond = LowerExpr(whileStmt.Cond);
                        var allLocalsNames = allLocals.Select(x => x.Key).ToList();
                        var allLocalsValues = allLocals.Select(x => x.Value).ToArray();
                        Builder.BuildBranch(cond, bodyBlock, allLocalsValues, continueBlock, allLocalsValues);
                        // switch to body
                        using (var _ = Symbols.EnterNewScope())
                        {
                            DoContextSwitch(allLocalsNames, bodyBlock);
                            Builder.SwitchBlock(bodyBlock);
                            foreach (var stmt1 in whileStmt.Body)
                            {
                                LowerStmt(stmt1);
                            }
                            // compile the condition (again)
                            var cond1 = LowerExpr(whileStmt.Cond);
                            var localsValues1 = allLocalsNames.Select(name => Symbols[name]).ToArray();
                            Builder.BuildBranch(cond1, bodyBlock, localsValues1, continueBlock, localsValues1);
                        }
                        // now the continue block
                        Symbols.ClearCurrentScope();
                        DoContextSwitch(allLocalsNames, continueBlock);
                        Builder.SwitchBlock(continueBlock);
                        return;
                    }
                case MatchStmt matchStmt:
                    {
                        // assign scrutinee to a local
                        var scrExpr = LowerExpr(matchStmt.Scrutinee);
                        var scrName = "*scr" + Random.Shared.Next(10000).ToString();
                        Symbols.AddNew(scrName, scrExpr);
                        // save locals
                        var allLocals = GetAllLocals();
                        var localsTypes = allLocals.Select(x => x.Value.GetType()).ToList();
                        var localsNames = allLocals.Select(x => x.Key).ToList();
                        var terminationBlock = currentFunc!.NewBlock(localsTypes);
                        LowerMatchRecursively(scrName, localsTypes, localsNames, terminationBlock.BlockId, matchStmt.Cases);
                        // go to the termination block
                        Symbols.ClearCurrentScope();
                        DoContextSwitch(localsNames, terminationBlock);
                        Builder.SwitchBlock(terminationBlock);
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private void LowerMatchRecursively(string scrName, List<TypeRef> localsTypes, List<string> localsNames,
            int terminationBlockId,
            IEnumerable<(MatchStmt.Patt, Stmt[])> cases)
        {
            if (!cases.Any())
            {
                // just jump to termination
                Builder.BuildGoto(terminationBlockId, localsNames.Select(x => Symbols[x]).ToArray());
                return;
            }
            var (patt, body) = cases.First();
            // compile condition
            var cond = LowerPattCond(patt, Symbols.Get(scrName));
            var bodyBlock = currentFunc!.NewBlock(localsTypes);
            var restBlock = currentFunc!.NewBlock(localsTypes);
            var localsValues = localsNames.Select(x => Symbols[x]).ToArray();
            Builder.BuildBranch(cond, bodyBlock, localsValues, restBlock, localsValues);
            // compile body
            using (_ = Symbols.EnterNewScope())
            {
                DoContextSwitch(localsNames, bodyBlock);
                Builder.SwitchBlock(bodyBlock);

                LowerPattBindings(patt, Symbols.Get(scrName));
                foreach (var stmt_ in body)
                    LowerStmt(stmt_);

                Builder.BuildGoto(terminationBlockId, localsNames.Select(x => Symbols[x]).ToArray());
            }
            // compile the `rest`
            using (_ = Symbols.EnterNewScope())
            {
                DoContextSwitch(localsNames, restBlock);
                Builder.SwitchBlock(restBlock);

                LowerMatchRecursively(scrName, localsTypes, localsNames, terminationBlockId, cases.Skip(1));
            }
        }

        private MidValue LowerPattCond(MatchStmt.Patt patt, MidValue scr)
        {
            switch (patt)
            {
                case MatchStmt.AnyPatt:
                case MatchStmt.NamePatt:
                    // no conditions, always succeeds
                    return MidValue.ConstBool(true);
                case MatchStmt.IntLitPatt ilp:
                    return Builder.BuildICmp(scr, MidValue.ConstInt(ilp.Val), MidICmpInstr.CmpOp.Eq);
                case MatchStmt.ObjectPatt objp:
                    if (objp.ClsType!.IsStructClass)
                    {
                        // itself no condition, but the subpatterns may have
                        {
                            var cond = MidValue.ConstBool(true);
                            for (int i = 0; i < objp.Members.Length; i++)
                            {
                                var memberPatt = objp.Members[i];
                                var member = Builder.BuildLoad(new FieldInfo(objp.ClsType!, i), scr);
                                cond = Builder.BuildAnd(cond, LowerPattCond(memberPatt, member));
                            }
                            return cond;
                        }
                    }
                    else if (objp.ClsType!.IsEnumClass)
                    {
                        // check the tag
                        var tag = Builder.BuildGetTag(scr);
                        var expectedTag = Array.FindIndex(objp.ClsType.Variants, (x) => x.VariantName == objp.ObjName);
                        if (expectedTag == -1) throw new Exception();
                        var cond = Builder.BuildICmp(tag, MidValue.ConstInt(expectedTag), MidICmpInstr.CmpOp.Eq);
                        // subpattern conditions
                        for (int i = 0; i < objp.Members.Length; i++)
                        {
                            var memberPatt = objp.Members[i];
                            var member = Builder.BuildLoad(new FieldInfo(objp.ClsType!, i, expectedTag), scr);
                            cond = Builder.BuildAnd(cond, LowerPattCond(memberPatt, member));
                        }
                        return cond;
                    }
                    else throw new Exception();
                default:
                    throw new NotImplementedException();
            }
        }

        private void LowerPattBindings(MatchStmt.Patt patt, MidValue scr)
        {
            switch (patt)
            {
                case MatchStmt.AnyPatt:
                case MatchStmt.IntLitPatt:
                    // no bindings
                    return;
                case MatchStmt.NamePatt np:
                    // binding
                    Symbols.AddNew(np.Name, scr);
                    return;
                case MatchStmt.ObjectPatt objp:
                    // no binding, subfields may be bound
                    var variantTag = objp.ClsType!.IsStructClass ? -1 :
                                        Array.FindIndex(objp.ClsType.Variants, (x) => x.VariantName == objp.ObjName);
                    for (int i = 0; i < objp.Members.Length; i++)
                    {
                        var memberPatt = objp.Members[i];
                        var member = Builder.BuildLoad(new FieldInfo(objp.ClsType!, i, variantTag), scr);
                        LowerPattBindings(memberPatt, member);
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }
        }

        private List<KeyValuePair<string, MidValue>> GetAllLocals() => Symbols.Flatten(1).ToList();

        /// <summary>
        /// This binds all block arguments to names.
        /// </summary>
        /// <param name="Names"></param>
        /// <param name="block"></param>
        private void DoContextSwitch(IEnumerable<string> Names, MidBlock block)
        {
            int i = 0;
            foreach (var name in Names)
            {
                Symbols.AddNew(name, block.Argument(i));
                i++;
            }
        }
    }
}
