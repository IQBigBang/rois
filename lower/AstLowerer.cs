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

        public AstLowerer()
        {
            var block = new MidBlock(0);
            Builder = new MidBuilder(block);
            Locals = new Dictionary<string, MidValue>();
        }

        public MidBlock GetBlock() => Builder.CurrentBlock;

        public void LowerFunc(Func f)
        {
            var block = new MidBlock(0, f.Arguments.Select(x => x.Item2).ToList());
            Builder.SwitchBlock(block);
            Locals = new Dictionary<string, MidValue>();
            // add arguments
            for (int i = 0; i < f.Arguments.Length; i++)
            {
                Locals.Add(f.Arguments[i].Item1, block.Argument(i));
            }
            // compile the body
            foreach (var stmt in f.Body)
            {
                LowerStmt(stmt);
            }
            Builder.BuildRet();
            block.Dump();
        }

        MidValue LowerExpr(ast.Expr expr)
        {
            switch (expr)
            {
                case ast.IntExpr intExpr:
                    return MidValue.ConstInt(intExpr.Value);
                case ast.VarExpr varExpr:
                    return Locals[varExpr.Name];
                case ast.BinOpExpr binOpExpr:
                    {
                        var lhs = LowerExpr(binOpExpr.Lhs);
                        var rhs = LowerExpr(binOpExpr.Rhs);
                        if (binOpExpr.Op == BinOpExpr.Ops.Add)
                        {
                            return Builder.BuildIAdd(lhs, rhs);
                        }
                        else throw new NotImplementedException();
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
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
