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
            lhs.AssertType(TypeRef.INT);
            rhs.AssertType(TypeRef.INT);
            var instr = new mid_ir.MidIAddInstr { Out = MidValue.Null(), Lhs = lhs, Rhs = rhs };
            return currentBlock.AddInstr(instr);
        }

        public MidValue BuildRet() => BuildRet(MidValue.Null());
        public MidValue BuildRet(MidValue val)
        {
            var instr = new mid_ir.MidIRet { Value = val };
            return currentBlock.AddInstr(instr);
        }
    }
}
