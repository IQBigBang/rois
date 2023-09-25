﻿using RoisLang.mid_ir;
using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm.c
{
    internal class CCompile
    {
        private TextWriter _out;
        private MidFunc? currentFunc;

        public CCompile(TextWriter output)
        {
            _out = output;
        }

        public void CompileModule(MidModule module)
        {
            _out.WriteLine("#include \"std/core.h\"");
            _out.WriteLine("#include \"std/alloc.h\"");

            // First the type definitions
            foreach (var cls in module.Classes)
                _out.WriteLine($"typedef struct struct_{cls.Name}* c_{cls.Name};");
            foreach (var cls in module.Classes)
            {
                _out.WriteLine($"struct struct_{cls.Name} {{");
                foreach (var field in cls.Fields)
                    _out.WriteLine($"{PrintTy(field.Item2)} {field.Item1};");
                _out.WriteLine("};");
            }
            // Function declarations
            foreach (var func in module.Functions) {
                var ftype = (FuncType)func.FuncType;
                if (func.IsExtern) _out.Write("extern ");
                _out.Write($"{PrintTy(ftype.Ret)} {func.Name}(");
                for (int i = 0; i < ftype.Args.Count; i++)
                {
                    if (i != 0) _out.Write(", ");
                    _out.Write(PrintTy(ftype.Args[i]));
                }
                _out.WriteLine(");");
            }
            foreach (var func in module.Functions)
                compileFunction(func);
            _out.Flush();
        }

        private void compileFunction(MidFunc func)
        {
            if (func.IsExtern) return;
            currentFunc = func;
            var ftype = (FuncType)func.FuncType;
            _out.Write($"{PrintTy(ftype.Ret)} {func.Name}(");
            for (int i = 0; i < ftype.Args.Count; i++)
            {
                if (i != 0) _out.Write(", ");
                _out.Write(PrintTy(ftype.Args[i]));
                _out.Write(" ");
                _out.Write(Print(func.EntryBlock.Arguments[i])); // function arguments = first block arguments
            }
            _out.WriteLine(") {");
            // Because our IR is a single-assignment form, we can declare values on-the-go
            // as we have the guarantee of them not being declared twice
            // however this does not apply for block arguments, which are "assigned" multiple times in a function
            // therefore we have to declare all block arguments at the beginning
            for (int i = 1/*skip entry block*/; i < func.Blocks.Count; i++)
                foreach (var arg in func.Blocks[i].Arguments)
                    _out.WriteLine($"{Declare(arg)};");
            foreach (var block in func.Blocks)
                compileBlock(block);
            _out.WriteLine("}");
        }

        private void compileBlock(MidBlock block)
        {
            _out.WriteLine($"bb{block.BlockId}: ;");
            foreach (var instr in block.Instrs)
            {
                if (instr != null) compileInstr(instr);
            }
        }

        private void compileInstr(MidInstr instr)
        {
            switch (instr)
            {
                case MidIAddInstr iAddInstr:
                    _out.WriteLine($"{Declare(iAddInstr.Out)} = ((I32){Print(iAddInstr.Lhs)}) + ((I32){Print(iAddInstr.Rhs)});");
                    break;
                case MidISubInstr iSubInstr:
                    _out.WriteLine($"{Declare(iSubInstr.Out)} = ((I32){Print(iSubInstr.Lhs)}) - ((I32){Print(iSubInstr.Rhs)});");
                    break;
                case MidIMulInstr iMulInstr:
                    _out.WriteLine($"{Declare(iMulInstr.Out)} = ((I32){Print(iMulInstr.Lhs)}) * ((I32){Print(iMulInstr.Rhs)});");
                    break;
                case MidRetInstr retInstr:
                    if (!retInstr.Value.IsNull) _out.WriteLine($"return {Print(retInstr.Value)};");
                    else _out.WriteLine("return;");
                    break;
                case MidCallInstr callInstr:
                    if (!callInstr.Out.IsNull) _out.Write($"{Declare(callInstr.Out)} = ");
                    _out.Write($"({Print(callInstr.Callee)})(");
                    for (int i = 0; i < callInstr.Arguments.Length; i++)
                    {
                        if (i != 0) _out.Write(", ");
                        _out.Write(Print(callInstr.Arguments[i]));
                    }
                    _out.WriteLine(");");
                    break;
                case MidICmpInstr icmpInstr:
                    _out.Write($"{Declare(icmpInstr.Out)} = ((I32){Print(icmpInstr.Lhs)}) ");
                    _out.Write(icmpInstr.Op switch { 
                        MidICmpInstr.CmpOp.Eq => "==", 
                        MidICmpInstr.CmpOp.NEq => "!=", 
                        MidICmpInstr.CmpOp.Lt => "<", 
                        MidICmpInstr.CmpOp.Le => "<=", 
                        MidICmpInstr.CmpOp.Gt => ">", 
                        MidICmpInstr.CmpOp.Ge => ">=", 
                    });
                    _out.WriteLine($"((I32){Print(icmpInstr.Rhs)});");
                    break;
                case MidGotoInstr gotoInstr:
                    // move the values
                    {
                        var targetBlock = currentFunc!.Blocks[gotoInstr.TargetBlockId];
                        for (int i = 0; i < gotoInstr.Arguments.Length; i++)
                            _out.WriteLine($"{Print(targetBlock.Arguments[i])} = {Print(gotoInstr.Arguments[i])};");
                        _out.WriteLine($"goto bb{targetBlock.BlockId};");
                    }
                    break;
                case MidBranchInstr branchInstr:
                    {
                        _out.WriteLine($"if ({Print(branchInstr.Cond)} == true) {{");
                        var targetBlockIf = currentFunc!.Blocks[branchInstr.Then.TargetBlockId];
                        for (int i = 0; i < branchInstr.Then.Arguments.Length; i++)
                            _out.WriteLine($"{Print(targetBlockIf.Arguments[i])} = {Print(branchInstr.Then.Arguments[i])};");
                        _out.WriteLine($"goto bb{targetBlockIf.BlockId};");
                        _out.WriteLine("}");
                        var targetBlockElse = currentFunc!.Blocks[branchInstr.Else.TargetBlockId];
                        for (int i = 0; i < branchInstr.Else.Arguments.Length; i++)
                            _out.WriteLine($"{Print(targetBlockElse.Arguments[i])} = {Print(branchInstr.Else.Arguments[i])};");
                        _out.WriteLine($"goto bb{targetBlockElse.BlockId};");
                    }
                    break;
                case MidLoadInstr loadInstr:
                    {
                        var classType = loadInstr.FieldInfo.Class;
                        var fieldName = loadInstr.FieldInfo.FieldName();
                        _out.WriteLine($"{Declare(loadInstr.Out)} = (({PrintTy(classType)}){Print(loadInstr.Object)})->{fieldName};");
                    }
                    break;
                case MidStoreInstr storeInstr:
                    {
                        var classType = storeInstr.FieldInfo.Class;
                        var fieldName = storeInstr.FieldInfo.FieldName();
                        var fieldType = storeInstr.Value.GetType();
                        _out.WriteLine($"(({PrintTy(classType)}){Print(storeInstr.Object)})->{fieldName} = (({PrintTy(fieldType)}){Print(storeInstr.Value)});");
                    }
                    break;
                case MidAllocClassInstr allocInstr:
                    {
                        var structType = "struct struct_" + allocInstr.Class.Name;
                        _out.WriteLine($"{Declare(allocInstr.Out)} = ({PrintTy(allocInstr.Class)})_halloc(sizeof({structType}));");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private string Declare(MidValue val)
            => PrintTy(val.GetType()) + " " + Print(val);

        private string Print(MidValue val)
        {
            if (val.IsConstInt) return $"((I32){val.GetIntValue()})";
            if (val.IsConstBool) return val.GetBoolValue() ? "true" : "false";
            if (val.IsGlobal) return val.GetGlobalValue().Name;
            if (val.IsReg) return $"_L{val.GetBasicBlock()}_{val.GetRegNum()}";
            throw new NotSupportedException();
        }

        private string PrintTy(TypeRef ty)
            => ty switch
            {
                IntType => "I32",
                BoolType => "bool",
                VoidType => "void", // TODO
                ClassType cls => $"c_{cls.Name}",
                _ => throw new NotImplementedException()
            };
    }
}
