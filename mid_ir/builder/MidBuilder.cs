using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir.builder
{
    public class MidBuilder
    {
        private MidBlock? currentBlock;

        public MidBuilder()
        {
            currentBlock = null;
        }

        public void SwitchBlock(MidBlock block)
        {
            currentBlock = block;
        }

        public MidBlock? CurrentBlock => currentBlock;

        /// <summary>
        /// reg = IAdd lhs, rhs
        /// </summary>
        public MidValue BuildIAdd(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new mid_ir.MidIAddInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr);
        }

        public MidValue BuildISub(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new mid_ir.MidISubInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr);
        }

        public MidValue BuildIMul(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new MidIMulInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr);
        }

        public void BuildRet() => BuildRet(MidValue.Null());
        public void BuildRet(MidValue val)
        {
            var instr = new mid_ir.MidRetInstr { Value = val };
            currentBlock!.AddInstr(instr);
        }

        public MidValue BuildCall(MidValue callee, MidValue[] args)
        {
            var instr = new MidCallInstr { Out = MidValue.Null(), Callee = callee, Arguments = args };
            return currentBlock!.AddInstr(instr);
        }

        public MidValue BuildICmp(MidValue lhs, MidValue rhs, MidICmpInstr.CmpOp op)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new MidICmpInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs, Op = op };
            return currentBlock!.AddInstr(instr);
        }

        public void BuildGoto(MidBlock targetBlock, MidValue[] args)
        {
            var instr = new MidGotoInstr { TargetBlockId = targetBlock.BlockId, Arguments = args };
            currentBlock!.AddInstr(instr);
        }

        public MidBranchInstr BuildBranch(MidValue condition, MidBlock thenTargetBlock, MidValue[] thenArgs, MidBlock elseTargetBlock, MidValue[] elseArgs)
        {
            var thenGoto = new MidGotoInstr { TargetBlockId = thenTargetBlock.BlockId, Arguments = thenArgs };
            var elseGoto = new MidGotoInstr { TargetBlockId = elseTargetBlock.BlockId, Arguments = elseArgs };
            var instr = new MidBranchInstr { Cond = condition, Then = thenGoto, Else = elseGoto };
            currentBlock!.AddInstr(instr);
            return instr;
        }
    }
}
