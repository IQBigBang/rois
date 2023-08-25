using RoisLang.types;
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
        public List<TypeRef> arguments;
        public List<MidInstr?> Instrs;

        private uint NextInstrRegIdx => (uint)(arguments.Count + Instrs.Count);

        public MidBlock(uint blockId, List<TypeRef>? arguments = null)
        {
            this.arguments = arguments ?? new List<TypeRef>();
            this.blockId = blockId;
            Instrs = new List<MidInstr?>();
        }
        
        public IEnumerable<MidValue> Arguments()
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                yield return MidValue.Reg((uint)i, blockId, arguments[i]);
            }
        }

        public MidValue Argument(int i)
        {
            if (i >= arguments.Count) return MidValue.Null();
            return MidValue.Reg((uint)i, blockId, arguments[i]);
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

            if (instr.HasOut())
            {
                var newReg = MidValue.Reg(NextInstrRegIdx, blockId, instr.OutType());
                instr.SetOut(newReg);
                Instrs.Add(instr);
                return newReg;
            }
            else
            {
                Instrs.Add(instr);
                return MidValue.Null();
            }
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
