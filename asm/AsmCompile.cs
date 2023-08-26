using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    public class AsmCompile
    {
        public static void CompileBlock(TextWriter output, MidBlock block)
        {
            // First, do RegAlloc
            var regAllocs = new RegAlloc().RegAllocBlock(block);
            // Then, initialize AsmWriter
            var asmWriter = new AsmWriter(output, regAllocs);

            // write the block name in the assembly
            asmWriter.WriteLn("bb{}:", block.BlockId.ToString());
            // if the block is the entry block, we have to do `mov`s from argument registers to assigned registers
            if (block.BlockId == 0)
            {
                // Windows x64 calling convention = arguments in rcx, rdx, r8, r9 (in this order)
                GpReg[] argumentRegs = new GpReg[] { GpReg.Rcx, GpReg.Rdx, GpReg.R8, GpReg.R9 };
                asmWriter.WriteMoves(argumentRegs, block.Arguments().ToArray());
            }
            // then, compile the instructions
            foreach (var instr in block.Instrs)
            {
                if (instr == null) continue;
                asmWriter.WriteAsmInstr(instr);
            }
        }
    }
}
