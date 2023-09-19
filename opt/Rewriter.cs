using RoisLang.mid_ir;
using RoisLang.mid_ir.builder;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.opt
{
    abstract class Rewriter
    {
        protected MidBlock? CurrentBlock;

        protected void ReplaceInstr(int instrIdx, Action<MidBuilder> newInstr)
        {
            var b = new MidBuilder();
            b.SwitchBlock(CurrentBlock!);
            b.SwitchToReplaceMode(instrIdx);
            newInstr(b);
        }

        protected void ReplaceInstrWithConstant(MidValue oldOutput, MidValue constant)
        {
            RemoveInstr((int)oldOutput.GetRegNum() - CurrentBlock!.Arguments.Count);
            ReplaceAll(oldOutput, constant);
        }

        protected void ReplaceAll(MidValue what, MidValue replacement)
        {
            foreach (var instr in CurrentBlock!.Instrs)
            {
                if (instr is null) continue;
                instr.Replace(what, replacement);
            }
        }

        public void RemoveArgument(int idx)
        {
            // removing an argument means shifting all indexes
            Shift((uint)idx);
        }

        protected void RemoveInstr(int instructionIdx)
        {
            CurrentBlock!.Instrs[instructionIdx] = null;
        }

        private void Shift(uint removedRegister)
        {
            Debug.Assert(removedRegister < (uint)CurrentBlock!.Arguments.Count); // TODO
            for (uint i = removedRegister; i < (uint)CurrentBlock!.Arguments.Count - 1; i++)
            {
                CurrentBlock!.Arguments[(int)i] = MidValue.Reg(i, 
                    (uint)CurrentBlock.BlockId, 
                    CurrentBlock.Arguments[(int)(i + 1)].GetType(), 
                    Assertion.X);
            }
            CurrentBlock!.Arguments.RemoveAt(CurrentBlock!.Arguments.Count - 1);
            for (int i = 0; i < CurrentBlock.Instrs.Count; i++)
            {
                if (CurrentBlock.Instrs[i] is null) continue;
                var previousOut = CurrentBlock.Instrs[i]!.GetOut();
                CurrentBlock.Instrs[i]!.SetOut(MidValue.Reg((uint)i - 1, (uint)CurrentBlock.BlockId, previousOut.GetType(), Assertion.X));
            }
            CurrentBlock.UpdateReferences();
        }
    }
}
