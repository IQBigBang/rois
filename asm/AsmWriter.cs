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
        StructLayout structLayout;
        /// <summary>
        /// Allocated physical registers
        /// </summary>
        Dictionary<MidValue, GpReg> regs;
        MidFunc function;

        public AsmWriter(TextWriter output, Dictionary<MidValue, GpReg> regs, MidFunc function, StructLayout structLayout)
        {
            this.output = output;
            this.regs = regs;
            this.function = function;
            this.structLayout = structLayout;
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
                        WriteLn("mov {b64}, {b64}", movIR.dest, movIR.value.ToString());
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
                case MidIMulInstr iMulInstr:
                    {
                        // IAdd is side-effect free, if its result is unused, it can be skipped
                        if (regs[iMulInstr.Out] == GpReg.RNull) break;
                        // if one of the operands is an integer value, use the special form of the `imul` instruction
                        if (iMulInstr.Lhs.IsConstInt || iMulInstr.Rhs.IsConstInt)
                        {
                            var intValue = iMulInstr.Lhs.IsConstInt ? iMulInstr.Lhs.GetIntValue() : iMulInstr.Rhs.GetIntValue();
                            var regValue = iMulInstr.Lhs.IsReg ? iMulInstr.Lhs : iMulInstr.Rhs;
                            // imul out, reg, imm
                            WriteLn("imul {b32}, {b32}, {b32}", iMulInstr.Out, regValue, intValue.ToString());
                        } else
                        {
                            // both are registers -> do the same thing as with addition
                            // mov out, lhs; imul out, rhs
                            if (iMulInstr.Rhs.IsReg && regs[iMulInstr.Rhs] == regs[iMulInstr.Out])
                            {
                                // out==rhs => imul out, lhs (addition is commutative)
                                WriteLn("imul {b32}, {b32}", iMulInstr.Out, iMulInstr.Lhs);
                                break;
                            }
                            // optimization, if out==lhs, the first mov is useless
                            if (iMulInstr.Lhs.IsReg && regs[iMulInstr.Lhs] == regs[iMulInstr.Out]) { }
                            else
                            {
                                // mov out, lhs
                                WriteLn("mov {b32}, {b32}", iMulInstr.Out, iMulInstr.Lhs);
                            }
                            // imul out, rhs
                            WriteLn("imul {b32}, {b32}", iMulInstr.Out, iMulInstr.Rhs);
                        }
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
                        // The Windows x64 cconv requires 32 bytes of shadow space
                        // we don't bother with this inside our own code, but if calling an extern function
                        // it MUST be done
                        if (callInstr.Callee.GetGlobalValue().IsExtern) WriteLn("sub rsp, 32");
                        // now the call
                        // TODO: before calling, the stack should be aligned at 16 bytes
                        WriteLn("call {addr}", callInstr.Callee);
                        if (callInstr.Callee.GetGlobalValue().IsExtern) WriteLn("add rsp, 32");
                        // if there is a result, move it into the correct register
                        if (!callInstr.Out.IsNull && (regs[callInstr.Out] != GpReg.RNull))
                            WriteLn("mov {b64}, rax", callInstr.Out);
                        // restore the registers
                        foreach (var lv in liveRegisters.Reverse<GpReg>())
                            WriteLn("pop {b64}", lv);
                    }
                    break;
                case MidICmpInstr icmpInstr:
                    {
                        // if output is unused, don't emit anything
                        if (regs[icmpInstr.Out] == GpReg.RNull) break;
                        // ICmp compiles to:
                        // cmp l, r
                        // setxx out
                        WriteLn("cmp {b32}, {b32}", icmpInstr.Lhs, icmpInstr.Rhs);
                        if (icmpInstr.Op is MidICmpInstr.CmpOp.Eq)
                            WriteLn("sete {b8}", icmpInstr.Out);
                        else if (icmpInstr.Op is MidICmpInstr.CmpOp.NEq)
                            WriteLn("setne {b8}", icmpInstr.Out);
                        else if (icmpInstr.Op is MidICmpInstr.CmpOp.Lt)
                            WriteLn("setl {b8}", icmpInstr.Out);
                        else if (icmpInstr.Op is MidICmpInstr.CmpOp.Le)
                            WriteLn("setle {b8}", icmpInstr.Out);
                        else if (icmpInstr.Op is MidICmpInstr.CmpOp.Gt)
                            WriteLn("setg {b8}", icmpInstr.Out);
                        else if (icmpInstr.Op is MidICmpInstr.CmpOp.Ge)
                            WriteLn("setge {b8}", icmpInstr.Out);
                    }
                    break;
                case MidGotoInstr gotoInstr:
                    {
                        // a `goto` is just shuffling of values followed by jmp
                        // `regs` should contain allocations for all registers in the function (incl. other blocks)
                        WriteMoves(gotoInstr.Arguments,
                                   function.Blocks[gotoInstr.TargetBlockId].Arguments.Select(x => regs[x]).ToArray());
                        WriteLn($"jmp {function.Name}_bb{gotoInstr.TargetBlockId}");
                    }
                    break;
                case MidBranchInstr branchInstr:
                    {
                        // a branch instruction compiles to:
                        // test rcond, rcond (this sets FLAGS.ZF to zero if true and one if false)
                        // jne rThen          (jumps to rThen if ZF is zero=condition is true)
                        WriteLn("test {b8}, {b8}", branchInstr.Cond, branchInstr.Cond);
                        string randName = "jmp_prepare" + Random.Shared.Next();
                        WriteLn($"jne {randName}"); // Jump to `Then`
                        // Now we're doing `Else`
                        // Shuffle the values (see GotoInstr)
                        WriteMoves(branchInstr.Else.Arguments,
                                   function.Blocks[branchInstr.Else.TargetBlockId].Arguments.Select(x => regs[x]).ToArray());
                        WriteLn($"jmp {function.Name}_bb{branchInstr.Else.TargetBlockId}");
                        // Now the `if` branch
                        WriteLn($"{randName}: ");
                        // Shuffle the values
                        WriteMoves(branchInstr.Then.Arguments,
                                   function.Blocks[branchInstr.Then.TargetBlockId].Arguments.Select(x => regs[x]).ToArray());
                        WriteLn($"jmp {function.Name}_bb{branchInstr.Then.TargetBlockId}");
                    }
                    break;
                case MidLoadInstr loadInstr:
                    {
                        if (regs[loadInstr.Out] == GpReg.RNull) break;
                        var fieldOffset = structLayout.GetFieldOffset(loadInstr.FieldInfo);
                        WriteLn("mov {b}, [{b64}+{}]", loadInstr.Out, loadInstr.Object, fieldOffset.ToString());
                    }
                    break;
                case MidStoreInstr storeInstr:
                    {
                        var fieldOffset = structLayout.GetFieldOffset(storeInstr.FieldInfo);
                        WriteLn("mov [{b64}+{}], {b}", storeInstr.Object, fieldOffset.ToString(), storeInstr.Value);
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
                if (formatSpecifiers.Contains("b") || formatSpecifiers.Contains("b32"))
                {
                    output.Write("dword ");
                    output.Write(midValue.GetIntValue());
                    return;
                }
            }
            if (midValue.IsConstBool)
            {
                if (formatSpecifiers.Contains("b64"))
                {
                    output.Write("qword " + (midValue.GetBoolValue() ? "1" : "0"));
                    return;
                }
                if (formatSpecifiers.Contains("b") || formatSpecifiers.Contains("b32"))
                {
                    output.Write("dword " + (midValue.GetBoolValue() ? "1" : "0"));
                    return;
                }
            }
            if (midValue.IsReg)
            {
                WriteGpReg(regs[midValue], formatSpecifiers, midValue.GetType());
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

        private void WriteGpReg(GpReg gpReg, string[] formatSpecifiers, types.TypeRef? type = null)
        {
            // `b` means variable-size based on the type
            // TODO: stop using b64,b32 use only `b`
            if (formatSpecifiers.Contains("b64") || (formatSpecifiers.Contains("b") && type!.GetRepr().Size() == 8))
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
            if (formatSpecifiers.Contains("b32") || (formatSpecifiers.Contains("b") && type!.GetRepr().Size() == 4))
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
            if (formatSpecifiers.Contains("b8"))
            {
                output.Write(gpReg switch
                {
                    GpReg.Rcx => "cl",
                    GpReg.Rdx => "dl",
                    GpReg.R8 => "r8b",
                    GpReg.R9 => "r9b",
                    GpReg.Rdi => "dil",
                    GpReg.Rsi => "sil",
                    GpReg.Rax => "al",
                    _ => throw new Exception(),
                });
                return;
            }
            throw new Exception("Invalid format");
        }
    }
}