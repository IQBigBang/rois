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
        public static void CompileAllFuncs(TextWriter output, IEnumerable<MidFunc> funcs)
        {
            output.WriteLine("\tdefault rel");
            foreach (var func in funcs)
                output.WriteLine($"global {func.Name}");
            output.WriteLine("section .text");
            output.WriteLine();
            foreach (var func in funcs)
            {
                CompileFunc(output, func);
                output.WriteLine();
            }
        }

        public static void CompileFunc(TextWriter output, MidFunc func)
        {
            output.WriteLine($"{func.Name}:");
            foreach (var block in func.Blocks)
                CompileBlock(output, func.Name, block);
            output.Flush();
        }

        private static void CompileBlock(TextWriter output, string fname, MidBlock block)
        {
            // First, do RegAlloc
            var regAllocs = new RegAlloc().RegAllocBlock(block);
            // We ATM depend on the block arguments always being allocated to specific registers
            for (int i = 0; i < block.Arguments.Count; i++)
            {
                if (regAllocs[block.Argument(i)] != (GpReg)i) throw new Exception("unexpected register allocation");
            }
            // Then, initialize AsmWriter
            var asmWriter = new AsmWriter(output, regAllocs, fname);

            // write the block name in the assembly
            asmWriter.WriteLn("{}_bb{}:", fname, block.BlockId.ToString());
            // if the block is the entry block, we have to do `mov`s from argument registers to assigned registers
            if (block.BlockId == 0)
            {
                if (block.Arguments.Count > 4) throw new Exception("More than 4 args not supported");
                // Windows x64 calling convention = arguments in rcx, rdx, r8, r9 (in this order)
                GpReg[] argumentRegs = new GpReg[] { GpReg.Rcx, GpReg.Rdx, GpReg.R8, GpReg.R9 };
                // moves from argument registers to allocated registers
                asmWriter.WriteMoves(block.Arguments.ToArray(), argumentRegs, true);
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
