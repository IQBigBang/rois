using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.opt
{
    internal class ConstantFold : Rewriter, IPass
    {
        public void RunOnBlock(MidBlock block)
        {
            CurrentBlock = block;
            for (int i = 0; i < block.Instrs.Count; i++)
            {
                switch (block.Instrs[i])
                {
                    case MidIAddInstr iAddInstr:
                        if (iAddInstr.Lhs.IsConstInt && iAddInstr.Rhs.IsConstInt)
                        {
                            int result = iAddInstr.Lhs.GetIntValue() + iAddInstr.Rhs.GetIntValue();
                            ReplaceInstrWithConstant(iAddInstr.Out, MidValue.ConstInt(result));
                        }
                        break;
                    case MidISubInstr iSubInstr:
                        if (iSubInstr.Lhs.IsConstInt && iSubInstr.Rhs.IsConstInt)
                        {
                            int result = iSubInstr.Lhs.GetIntValue() - iSubInstr.Rhs.GetIntValue();
                            ReplaceInstrWithConstant(iSubInstr.Out, MidValue.ConstInt(result));
                        }
                        break;
                    case MidIMulInstr iMulInstr:
                        if (iMulInstr.Lhs.IsConstInt && iMulInstr.Rhs.IsConstInt)
                        {
                            int result = iMulInstr.Lhs.GetIntValue() * iMulInstr.Rhs.GetIntValue();
                            ReplaceInstrWithConstant(iMulInstr.Out, MidValue.ConstInt(result));
                        }
                        break;
                    case MidICmpInstr icmpInstr:
                        if (icmpInstr.Lhs.IsConstInt && icmpInstr.Rhs.IsConstInt)
                        {
                            bool result = icmpInstr.Op switch
                            {
                                MidICmpInstr.CmpOp.Eq => icmpInstr.Lhs.GetIntValue() == icmpInstr.Rhs.GetIntValue(),
                                MidICmpInstr.CmpOp.NEq => icmpInstr.Lhs.GetIntValue() != icmpInstr.Rhs.GetIntValue(),
                                MidICmpInstr.CmpOp.Lt => icmpInstr.Lhs.GetIntValue() < icmpInstr.Rhs.GetIntValue(),
                                MidICmpInstr.CmpOp.Le => icmpInstr.Lhs.GetIntValue() <= icmpInstr.Rhs.GetIntValue(),
                                MidICmpInstr.CmpOp.Gt => icmpInstr.Lhs.GetIntValue() > icmpInstr.Rhs.GetIntValue(),
                                MidICmpInstr.CmpOp.Ge => icmpInstr.Lhs.GetIntValue() >= icmpInstr.Rhs.GetIntValue(),
                            };
                            ReplaceInstrWithConstant(icmpInstr.Out, MidValue.ConstBool(result));
                        }
                        break;
                    case MidBranchInstr branchInstr:
                        if (branchInstr.Cond.IsConst)
                        {
                            // Replace BranchInstr with Goto
                            if (branchInstr.Cond.GetBoolValue() == true)
                                ReplaceInstr(i, (builder) =>
                                    builder.BuildGoto(branchInstr.Then.TargetBlockId, branchInstr.Then.Arguments));
                            else
                                ReplaceInstr(i, (builder) =>
                                    builder.BuildGoto(branchInstr.Else.TargetBlockId, branchInstr.Else.Arguments));
                        }
                        break;
                    // Cannot be constant-folded
                    case MidGotoInstr _:
                    case MidRetInstr _:
                    case MidCallInstr _:
                    case MidStoreInstr _:
                    case MidLoadInstr _:
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
