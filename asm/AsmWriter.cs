﻿using RoisLang.mid_ir;
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

        // compile moves from certain physical registers to virtual registers or vice versa
        public void WriteMoves(GpReg[] from, MidValue[] to, bool reverse = false)
        {
            for (int i = 0; i < Math.Min(from.Length, to.Length); i++)
            {
                if (to[i].IsReg && regs[to[i]] == from[i]) continue; // from==to, no MOVing neccessary
                if (to[i].IsReg && regs[to[i]] == GpReg.RNull) continue; // `mov`ing to RNull => noop
                if (reverse)
                    // move to --> from
                    WriteLn("mov {b64}, {b64}", from[i], to[i]);
                else
                    // move from --> to
                    WriteLn("mov {b64}, {b64}", to[i], from[i]);
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
                case MidIRet iRetInstr:
                    {
                        if (!iRetInstr.Value.IsNull)
                            WriteLn("mov rax, {b64}", iRetInstr.Value);
                        WriteLn("ret");
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
                    string[] formatSpecifiers = format.Substring(i+1, endOfFormatSpecifier-(i+1)).Split(',');
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