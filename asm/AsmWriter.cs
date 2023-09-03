using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    public class AsmWriter
    {
        TextWriter output;
        /// <summary>
        /// Allocated physical registers
        /// </summary>
        Dictionary<MidValue, GpReg> regs;

        public AsmWriter(TextWriter output, Dictionary<MidValue, GpReg> regs)
        {
            this.output = output;
            this.regs = regs;
        }

        public const bool ASM_MOVES_USE_NEW_ALGORITHM = true;

        // compile moves from certain physical registers to virtual registers
        public void WriteMoves(MidValue[] from, GpReg[] to, bool reverse = false)
        {
            if (ASM_MOVES_USE_NEW_ALGORITHM)
            {
                // use the MovesAlgorithm
                var moves = MovesAlgorithm.CompileMoves(from, to, reverse, regs);
                foreach (var move in moves)
                {
                    if (move is MovesAlgorithm.MovIR movIR)
                        WriteLn("mov {b64}, {b64}", movIR.dest, movIR.value);
                    if (move is MovesAlgorithm.MovRR movRR)
                        WriteLn("mov {b64}, {b64}", movRR.dest, movRR.value);
                    if (move is MovesAlgorithm.Swap swap)
                        WriteLn("xchg {b64}, {b64}", swap.one, swap.two);
                }
            }
            else
            {
                // The old (incorrect!) approach
                for (int i = 0; i < Math.Min(from.Length, to.Length); i++)
                {
                    if (from[i].IsReg && regs[from[i]] == to[i]) continue; // from==to, no MOVing neccessary
                    if (from[i].IsReg && regs[from[i]] == GpReg.RNull) continue; // `mov`ing to/from RNull => noop
                    if (reverse)
                        // move to --> from
                        WriteLn("mov {b64}, {b64}", from[i], to[i]);
                    else
                        // move from --> to
                        WriteLn("mov {b64}, {b64}", to[i], from[i]);
                }
            }
        }

        public void WriteAsmInstr(MidInstr instr)
        {
            switch (instr)
            {
                case MidIAddInstr iAddInstr:
                    {
                        // IAdd is side-effect free, if its result is unused, it can be skipped
                        if (regs[iAddInstr.Out] == GpReg.RNull) break;
                        // IAdd generally compiles to:
                        // mov out, lhs
                        // add out, rhs
                        // the only thing we have to check for is if out==rhs, in which case a miscompilation would occur
                        if (iAddInstr.Rhs.IsReg && regs[iAddInstr.Rhs] == regs[iAddInstr.Out])
                        {
                            // out==rhs => add out, lhs (addition is commutative)
                            WriteLn("add {b32}, {b32}", iAddInstr.Out, iAddInstr.Lhs);
                            break;
                        }
                        // optimization, if out==lhs, the first mov is useless
                        if (iAddInstr.Lhs.IsReg && regs[iAddInstr.Lhs] == regs[iAddInstr.Out]) { }
                        else
                        {
                            // mov out, lhs
                            WriteLn("mov {b32}, {b32}", iAddInstr.Out, iAddInstr.Lhs);
                        }
                        // add out, rhs
                        WriteLn("add {b32}, {b32}", iAddInstr.Out, iAddInstr.Rhs);
                    }
                    break;
                case MidISubInstr iSubInstr:
                    {
                        // IAdd is side-effect free, if its result is unused, it can be skipped
                        if (regs[iSubInstr.Out] == GpReg.RNull) break;
                        // ISub generally compiles to:
                        // mov out, lhs
                        // sub out, rhs
                        // the only thing we have to check for is if out==rhs, in which case a miscompilation would occur
                        if (iSubInstr.Rhs.IsReg && regs[iSubInstr.Rhs] == regs[iSubInstr.Out])
                        {
                            // out==rhs => sub out, lhs
                            // correct the value by doing `neg out`
                            WriteLn("sub {b32}, {b32}", iSubInstr.Out, iSubInstr.Lhs);
                            WriteLn("neg {b32}", iSubInstr.Out);
                            break;
                        }
                        // optimization, if out==lhs, the first mov is useless
                        if (iSubInstr.Lhs.IsReg && regs[iSubInstr.Lhs] == regs[iSubInstr.Out]) { }
                        else
                        {
                            // mov out, lhs
                            WriteLn("mov {b32}, {b32}", iSubInstr.Out, iSubInstr.Lhs);
                        }
                        // sub out, rhs
                        WriteLn("sub {b32}, {b32}", iSubInstr.Out, iSubInstr.Rhs);
                    }
                    break;
                case MidRetInstr retInstr:
                    {
                        if (!retInstr.Value.IsNull)
                            WriteLn("mov rax, {b64}", retInstr.Value);
                        WriteLn("ret");
                    }
                    break;
                case MidCallInstr callInstr:
                    {
                        // first preserve registers
                        var liveRegisters = ((LiveRegData)callInstr.extra!).LiveRegisters;
                        foreach (var lv in liveRegisters)
                            WriteLn("push {b64}", lv);
                        // move arguments into registers
                        // Windows x64 calling convention = arguments in rcx, rdx, r8, r9 (in this order)
                        if (callInstr.Arguments.Length > 4) throw new Exception("More than 4 args not supported");
                        WriteMoves(callInstr.Arguments, new GpReg[] { GpReg.Rcx, GpReg.Rdx, GpReg.R8, GpReg.R9 });
                        // now the call
                        WriteLn("call {addr}", callInstr.Callee);
                        // if there is a result, move it into the correct register
                        if (!callInstr.Out.IsNull)
                            WriteLn("mov {b64}, rax", callInstr.Out);
                        // restore the registers
                        foreach (var lv in liveRegisters.Reverse<GpReg>())
                            WriteLn("pop {b64}", lv);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void WriteLn(string format, params object[] args)
        {
            Write(format, args);
            output.WriteLine();
        }

        public void Write(string format, params object[] args)
        {
            int argCounter = 0;
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '{') 
                {
                    int endOfFormatSpecifier = format.IndexOf('}', i);
                    string[] formatSpecifiers = format[(i + 1)..endOfFormatSpecifier].Split(',');
                    if (args[argCounter] is MidValue mv)
                        WriteValue(mv, formatSpecifiers);
                    else if (args[argCounter] is GpReg gpReg)
                        WriteGpReg(gpReg, formatSpecifiers);
                    else if (args[argCounter] is string s)
                        output.Write(s);
                    else
                        throw new ArgumentException();
                    argCounter++;
                    i = endOfFormatSpecifier;
                } else
                {
                    output.Write(format[i]);
                }
            }
        }

        private void WriteValue(MidValue midValue, string[] formatSpecifiers)
        {
            if (midValue.IsNull) throw new Exception();
            if (midValue.IsConstInt)
            {
                if (formatSpecifiers.Contains("b64"))
                {
                    output.Write("qword ");
                    output.Write(midValue.GetIntValue());
                    return;
                }
                if (formatSpecifiers.Contains("b32"))
                {
                    output.Write("dword ");
                    output.Write(midValue.GetIntValue());
                    return;
                }
            }
            if (midValue.IsReg)
            {
                WriteGpReg(regs[midValue], formatSpecifiers);
                return;
            }
            if (midValue.IsGlobal)
            {
                if (formatSpecifiers.Contains("addr"))
                {
                    output.Write(midValue.GetGlobalValue().Name);
                    return;
                }
            }
            throw new Exception("Invalid format");
        }

        private void WriteGpReg(GpReg gpReg, string[] formatSpecifiers)
        {
            if (formatSpecifiers.Contains("b64"))
            {
                output.Write(gpReg switch
                {
                    GpReg.Rcx => "rcx",
                    GpReg.Rdx => "rdx",
                    GpReg.R8 => "r8",
                    GpReg.R9 => "r9",
                    GpReg.Rdi => "rdi",
                    GpReg.Rsi => "rsi",
                    GpReg.Rax => "rax",
                    _ => throw new Exception(),
                });
                return;
            }
            if (formatSpecifiers.Contains("b32"))
            {
                output.Write(gpReg switch
                {
                    GpReg.Rcx => "ecx",
                    GpReg.Rdx => "edx",
                    GpReg.R8 => "r8d",
                    GpReg.R9 => "r9d",
                    GpReg.Rdi => "edi",
                    GpReg.Rsi => "esi",
                    GpReg.Rax => "eax",
                    _ => throw new Exception(),
                });
                return;
            }
            throw new Exception("Invalid format");
        }
    }
}