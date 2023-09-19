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

        public List<MidValue> Arguments => arguments;
        public List<MidInstr?> Instrs => instrs;

        // private uint NextInstrRegIdx => (uint)(arguments.Count + Instrs.Count);

        public MidBlock(uint blockId, List<TypeRef>? argumentTypes_ = null)
        {
            this.blockId = blockId;
            arguments = new List<MidValue>();
            instrs = new List<MidInstr?>();
            InitArguments(argumentTypes_ ?? new List<TypeRef>());
        }

        /// <summary>
        /// Set the argument types after creating the block.
        /// This is only possible if no instructions were written yet.
        /// </summary>
        /// <param name="argumentTypes"></param>
        public void InitArguments(List<TypeRef> argumentTypes)
        {
            if (instrs.Count != 0) throw new InvalidOperationException();
            arguments.Clear();
            for (int i = 0; i < argumentTypes.Count; i++)
            {
                arguments.Add(MidValue.Reg((uint)i, blockId, argumentTypes[i], Assertion.X));
            }
        }

        public MidValue Argument(int i) => Arguments[i];

        public MidValue AddInstr(MidInstr instr, int pos = -1)
        {
            // verification
            foreach (var val in instr.AllArgs())
            {
                if (val.IsReg)
                {
                    // verify the register is from this block
                    if (val.GetBasicBlock() != blockId)
                        throw new Exception("A foreign block value used in basic block");
                    // verify the register is well-defined
                    if (val.GetRegNum() > Arguments.Count + Instrs.Count)
                        throw new Exception("Undefined register used");
                }
            }

            if (pos == -1) pos = Instrs.Count;

            if (instr.HasOut())
            {
                var newReg = MidValue.Reg((uint)(Arguments.Count + pos), blockId, instr.OutType(), Assertion.X);
                instr.SetOut(newReg);
                if (pos == Instrs.Count) instrs.Add(instr);
                else instrs[pos] = instr;
                return newReg;
            }
            else
            {
                if (pos == Instrs.Count) instrs.Add(instr);
                else instrs[pos] = instr;
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

        public MidValue GetRegister(uint n)
        {
            if ((int)n < Arguments.Count) return Arguments[(int)n];
            else return Instrs[(int)(n - Arguments.Count)]?.GetOut() ?? MidValue.Null();
        }

        public void UpdateReferences()
        {
            foreach (var instr in Instrs)
            {
                if (instr is null) continue;
                instr.Map(val => val.IsReg ? GetRegister(val.GetRegNum()) : val);
            }
        }
    }
}
