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
        private int Pos;
        private MidBlock? currentBlock;

        public MidBuilder()
        {
            currentBlock = null;
        }

        public void SwitchBlock(MidBlock block)
        {
            currentBlock = block;
            Pos = currentBlock.Instrs.Count;
        }

        public void SwitchToReplaceMode(int where) => Pos = where;
        public void SwitchToAppendMode() => Pos = currentBlock!.Instrs.Count;

        private int IncrementPos { get { Pos += 1; return Pos - 1; } }

        public MidBlock? CurrentBlock => currentBlock;

        /// <summary>
        /// reg = IAdd lhs, rhs
        /// </summary>
        public MidValue BuildIAdd(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new mid_ir.MidIAddInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidValue BuildISub(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new mid_ir.MidISubInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidValue BuildIMul(MidValue lhs, MidValue rhs)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new MidIMulInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public void BuildRet() => BuildRet(MidValue.Null());
        public void BuildRet(MidValue val)
        {
            var instr = new mid_ir.MidRetInstr { Value = val };
            currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidValue BuildCall(MidValue callee, MidValue[] args)
        {
            var instr = new MidCallInstr { Out = MidValue.Null(), Callee = callee, Arguments = args };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidValue BuildICmp(MidValue lhs, MidValue rhs, MidICmpInstr.CmpOp op)
        {
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new MidICmpInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs, Op = op };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public void BuildGoto(MidBlock targetBlock, MidValue[] args) => BuildGoto(targetBlock.BlockId, args);
        public void BuildGoto(int targetBlockId, MidValue[] args)
        {
            var instr = new MidGotoInstr { TargetBlockId = targetBlockId, Arguments = args };
            currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidBranchInstr BuildBranch(MidValue condition, MidBlock thenTargetBlock, MidValue[] thenArgs, MidBlock elseTargetBlock, MidValue[] elseArgs)
            => BuildBranch(condition, thenTargetBlock.BlockId, thenArgs, elseTargetBlock.BlockId, elseArgs);
        public MidBranchInstr BuildBranch(MidValue condition, int thenTargetBlock, MidValue[] thenArgs, int elseTargetBlock, MidValue[] elseArgs)
        {
            var thenGoto = new MidGotoInstr { TargetBlockId = thenTargetBlock, Arguments = thenArgs };
            var elseGoto = new MidGotoInstr { TargetBlockId = elseTargetBlock, Arguments = elseArgs };
            var instr = new MidBranchInstr { Cond = condition, Then = thenGoto, Else = elseGoto };
            currentBlock!.AddInstr(instr, IncrementPos);
            return instr;
        }

        public MidValue BuildLoad(MidValue obj, string fieldName)
            => BuildLoad(new FieldInfo((ClassType)obj.GetType(), fieldName), obj);
        public MidValue BuildLoad(FieldInfo fieldInfo, MidValue obj)
        {
            var instr = new MidLoadInstr { FieldInfo = fieldInfo, Object = obj, Out = MidValue.Null() };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }

        public void BuildStore(MidValue obj, MidValue val, string fieldName)
            => BuildStore(new FieldInfo((ClassType)obj.GetType(), fieldName), obj, val);
        public void BuildStore(FieldInfo fieldInfo, MidValue obj, MidValue val)
        {
            var instr = new MidStoreInstr { FieldInfo = fieldInfo, Object = obj, Value = val };
            currentBlock!.AddInstr(instr, IncrementPos);
        }

        public MidValue BuildAllocClass(ClassType cls)
        {
            var instr = new MidAllocClassInstr { Class = cls, Out = MidValue.Null() };
            return currentBlock!.AddInstr(instr, IncrementPos);
        }
    }
}
