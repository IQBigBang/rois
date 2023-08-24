using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    /// <summary>
    /// A basic block.
    /// Every basic block is (because of SSA) essentially a list of definitions of values
    /// </summary>
    public class MidBlock
    {
        private readonly uint blockId;
        public uint argumentCount;
        public List<MidInstr?> Instrs;

        private uint NextInstrRegIdx => argumentCount + (uint)Instrs.Count;

        public MidBlock(uint blockId, int argumentCount)
        {
            this.argumentCount = (uint)argumentCount;
            this.blockId = blockId;
            Instrs = new List<MidInstr?>();
        }
        
        public IEnumerable<MidValue> Arguments()
        {
            for (uint i = 0; i < argumentCount; i++)
            {
                yield return MidValue.Reg(i, blockId);
            }
        }

        public MidValue Argument(int i)
        {
            if (i >= argumentCount) return MidValue.Null();
            return MidValue.Reg((uint)i, blockId);
        }

        public MidValue AddInstr(MidInstr instr)
        {
            // verification
            foreach (var val in instr.AllArgs())
            {
                if (val.IsReg) {
                    if (val.GetBasicBlock() != blockId)
                        throw new Exception("A foreign block value used in basic block");
                }
            }

            var newReg = MidValue.Reg(NextInstrRegIdx, blockId);
            instr.SetOut(newReg);
            Instrs.Add(instr);
            return newReg;
        }

        public void Dump()
        {
            Console.WriteLine($"BB{blockId}({string.Join(", ", Arguments())}):");
            foreach (var instr in Instrs)
            {
                if (instr == null) continue;
                Console.Write("  ");
                instr.Dump();
            }
        }
    }
}
