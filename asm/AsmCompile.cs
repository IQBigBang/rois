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
            StructLayout structLayout = new();
            output.WriteLine("\tdefault rel");
            foreach (var func in funcs)
            {
                if (func.IsExtern) output.WriteLine($"extern {func.Name}");
                else output.WriteLine($"global {func.Name}");
            }
            output.WriteLine("section .text");
            output.WriteLine();
            foreach (var func in funcs.Where(f => !f.IsExtern))
            {
                CompileFunc(output, func, structLayout);
                output.WriteLine();
            }
            structLayout.PrintRepresentations();
        }

        public static void CompileFunc(TextWriter output, MidFunc func, StructLayout structLayout)
        {
            // Because of jumping between blocks, we have to RegAlloc all blocks before
            // and we can just concatenate all of the dictionaries into one
            Dictionary<MidValue, GpReg> allRegAllocs = new();
            foreach (var block in func.Blocks)
                foreach (var kvp in new RegAlloc().RegAllocBlock(block))
                    allRegAllocs.Add(kvp.Key, kvp.Value); // this throws an exception on duplicate key, which is expected

            output.WriteLine($"{func.Name}:");
            foreach (var block in func.Blocks)
                CompileBlock(output, func, block, allRegAllocs, structLayout);
            output.Flush();
        }

        private static void CompileBlock(TextWriter output, MidFunc func, MidBlock block, Dictionary<MidValue, GpReg> regAllocs, StructLayout structLayout)
        {
            // Then, initialize AsmWriter
            var asmWriter = new AsmWriter(output, regAllocs, func, structLayout);

            // write the block name in the assembly
            asmWriter.WriteLn("{}_bb{}:", func.Name, block.BlockId.ToString());
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
