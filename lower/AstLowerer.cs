using RoisLang.ast;
using RoisLang.mid_ir;
using RoisLang.mid_ir.builder;
using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.lower
{
    public class AstLowerer
    {
        private MidBuilder Builder;
        private Dictionary<string, MidValue> Locals;
        private Dictionary<string, MidValue> Globals;

        public AstLowerer()
        {
            Builder = new MidBuilder();
            Locals = new Dictionary<string, MidValue>();
            Globals = new Dictionary<string, MidValue>();
        }

        public List<MidFunc> LowerProgram(ast.Program program)
        {
            List<MidFunc> midFuncs = new();
            foreach (var func in program.Functions)
            {
                if (Globals.ContainsKey(func.Name)) throw new Exception();
                var midFunc = new MidFunc(func.Name, func.Arguments.Select(x => x.Item2).ToList(), func.Ret);
                midFuncs.Add(midFunc);
                var value = MidValue.Global(midFunc, Assertion.X);
                Globals.Add(func.Name, value);
            }
            for (int i = 0; i < program.Functions.Length; i++)
                LowerFunc(program.Functions[i], midFuncs[i]);
            return midFuncs;
        }

        private void LowerFunc(Func f, MidFunc target)
        {
            Builder.SwitchBlock(target.EntryBlock);
            Locals = new Dictionary<string, MidValue>();
            // add arguments
            for (int i = 0; i < f.Arguments.Length; i++)
            {
                Locals.Add(f.Arguments[i].Item1, target.EntryBlock.Argument(i));
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
                case ast.VarExpr varExpr:
                    if (Locals.ContainsKey(varExpr.Name))
                        return Locals[varExpr.Name];
                    else if (Globals.ContainsKey(varExpr.Name))
                        return Globals[varExpr.Name];
                    else throw new Exception();
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = LowerExpr(binOpExpr.Lhs);
                        var rhs = LowerExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op == BinOpExpr.Ops.Add)
                        {
                            return Builder.BuildIAdd(lhs, rhs);
                        } else if (binOpExpr.Op == BinOpExpr.Ops.Sub)
                        {
                            return Builder.BuildISub(lhs, rhs);
                        }
                        else throw new NotImplementedException();
                    }
                case ast.CallExpr callExpr:
                    {
                        var callee = LowerExpr(callExpr.Callee);
                        return Builder.BuildCall(callee);
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
                        Locals[letAssignStmt.VarName] = value;
                        return;
                    }
                case ast.AssignStmt assignStmt:
                    {
                        var value = LowerExpr(assignStmt.Value);
                        if (assignStmt.Lhs is ast.VarExpr varExpr)
                        {
                            Locals[varExpr.Name] = value;
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
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
