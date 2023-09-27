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

        public AstLowerer()
        {
            Builder = new MidBuilder();
            Symbols = new ScopedDictionary<string, MidValue>();
        }

        public MidModule LowerProgram(ast.Program program)
        {
            Symbols.Reset();
            List<MidFunc> midFuncs = new();
            foreach (var func in program.Functions)
            {
                if (Symbols.Contains(func.Name)) throw new Exception();
                var midFunc = new MidFunc(func.Name, func.Arguments.Select(x => x.Item2).ToList(), func.Ret, func.Extern);
                midFuncs.Add(midFunc);
                var value = MidValue.Global(midFunc, Assertion.X);
                Symbols.AddNew(func.Name, value);
            }
            for (int i = 0; i < program.Functions.Length; i++)
                LowerFunc(program.Functions[i], midFuncs[i]);
            return new MidModule(midFuncs, program.Classes.Select(x => x.Type!).ToList());
        }

        private void LowerFunc(Func f, MidFunc target)
        {
            if (f.Extern) return;
            currentFunc = target;
            Builder.SwitchBlock(target.EntryBlock);
            using var _ = Symbols.EnterNewScope();
            // add arguments
            for (int i = 0; i < f.Arguments.Length; i++)
            {
                Symbols.AddNew(f.Arguments[i].Item1, target.EntryBlock.Argument(i));
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
                case ast.VarExpr varExpr:
                    if (Symbols.Contains(varExpr.Name))
                        return Symbols[varExpr.Name];
                    else throw new Exception();
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
                        var instance = Builder.BuildAllocClass(constrExpr.Class);
                        foreach (var (fieldName, fieldExpr) in constrExpr.Fields)
                        {
                            var fieldValue = LowerExpr(fieldExpr);
                            Builder.BuildStore(instance, fieldValue, fieldName);
                        }
                        return instance;
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
                        else throw new Exception();
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
