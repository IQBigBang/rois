using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir.builder
{
    public class MidBuilder
    {
        private MidBlock currentBlock;

        public MidBuilder(MidBlock block)
        {
            currentBlock = block;
        }

        public void SwitchBlock(MidBlock block)
        {
            currentBlock = block;
        }

        /// <summary>
        /// reg = IAdd lhs, rhs
        /// </summary>
        public MidValue BuildIAdd(MidValue lhs, MidValue rhs)
        {
            var instr = new mid_ir.MidIAddInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock.AddInstr(instr);
        }
    }
}
