using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.opt
{
    /// <summary>
    /// Removes unreachable instructions AND unreachable blocks
    /// </summary>
    internal class RemoveDeadCode : Rewriter, IPass
    {
        public void RunOnFunction(MidFunc func)
        {
            foreach (var block in func.Blocks)
            {
                RunOnBlock(block);
            }
            // TODO: remove unreachable blocks
        }

        public void RunOnBlock(MidBlock block)
        {
            CurrentBlock = block;
            for (int i = 0; i < block.Instrs.Count; i++)
            {
                var instr = block.Instrs[i];
                if (instr is null) continue;
                // these are terminating instructions
                if (instr is MidRetInstr or MidGotoInstr or MidBranchInstr)
                {
                    // any following instructions may be freely removed
                    block.Instrs.RemoveRange(i + 1, block.Instrs.Count - (i + 1));
                    break;
                }
            }
        }
    }
}
