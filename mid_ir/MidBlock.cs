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
        private readonly List<MidValue> arguments;
        private readonly List<MidInstr?> instrs;

        public IReadOnlyList<MidValue> Arguments => arguments;
        public IReadOnlyList<MidInstr?> Instrs => instrs;

        private uint NextInstrRegIdx => (uint)(arguments.Count + Instrs.Count);

        public MidBlock(uint blockId, List<TypeRef>? argumentTypes_ = null)
        {
            var argumentTypes = argumentTypes_ ?? new List<TypeRef>();
            this.arguments = new List<MidValue>();
            for (int i = 0; i < argumentTypes.Count; i++)
            {
                arguments.Add(MidValue.Reg((uint)i, blockId, argumentTypes[i], Assertion.X));
            }
            this.blockId = blockId;
            instrs = new List<MidInstr?>();
        }

        public MidValue Argument(int i) => Arguments[i];

        public MidValue AddInstr(MidInstr instr)
        {
            // verification
            foreach (var val in instr.AllArgs())
            {
                if (val.IsReg) {
                    // verify the register is from this block
                    if (val.GetBasicBlock() != blockId)
                        throw new Exception("A foreign block value used in basic block");
                    // verify the register is well-defined
                    if (val.GetRegNum() > NextInstrRegIdx)
                        throw new Exception("Undefined register used");
                }
            }

            if (instr.HasOut())
            {
                var newReg = MidValue.Reg(NextInstrRegIdx, blockId, instr.OutType(), Assertion.X);
                instr.SetOut(newReg);
                instrs.Add(instr);
                return newReg;
            }
            else
            {
                instrs.Add(instr);
                return MidValue.Null();
            }
        }

        public void Dump()
        {
            Console.WriteLine($"BB{blockId}({string.Join(", ", Arguments)}):");
            foreach (var instr in Instrs)
            {
                if (instr == null) continue;
                Console.Write("  ");
                instr.Dump();
            }
        }

        public IEnumerable<MidValue> AllRegisters()
        {
            foreach (var arg in Arguments)
            {
                yield return arg;
            }

            foreach (var instr in Instrs)
            {
                if (instr != null && instr.HasOut())
                {
                    yield return instr.GetOut();
                }
            }
        }

        public int BlockId => (int)blockId;
    }
}
