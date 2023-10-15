using RoisLang.mid_ir;
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

            // Type definitions before function types
            foreach (var cls in module.UserTypes)
                _out.WriteLine($"typedef struct struct_{cls.Name}* {NameMangle.NameType(cls)};");
            // Function types
            foreach (var ftype in FuncType.AllFuncTypes)
            {
                _out.WriteLine("typedef struct {");
                _out.Write($"{PrintTy(ftype.Ret)} (*fptr)(");
                for (int i = 0; i < ftype.Args.Count; i++)
                {
                    if (i != 0) _out.Write(", ");
                    _out.Write(PrintTy(ftype.Args[i]));
                }
                _out.WriteLine(");");
                _out.WriteLine("void* env;");
                _out.WriteLine($"}} {PrintTy(ftype)};");
            }
            foreach (var cls in module.UserTypes)
            {
                if (cls.IsStructClass)
                {
                    _out.WriteLine($"struct struct_{cls.Name} {{");
                    foreach (var field in cls.Fields)
                        _out.WriteLine($"{PrintTy(field.Item1)} {field.Item2};");
                    _out.WriteLine("};");
                } else if (cls.IsEnumClass)
                {
                    _out.WriteLine($"struct struct_{cls.Name} {{");
                    _out.WriteLine("I32 tag;");
                    _out.WriteLine("union {");
                    foreach (var variant in cls.Variants)
                    {
                        _out.WriteLine("struct {");
                        foreach (var field in variant.Fields)
                            _out.WriteLine($"{PrintTy(field.Item1)} {field.Item2};");
                        _out.WriteLine($"}} {variant.VariantName};");
                    }
                    _out.WriteLine("} payload;");
                    _out.WriteLine("};");
                }
            }
            // Function declarations
            foreach (var func in module.Functions) {
                var ftype = (FuncType)func.FuncType;
                if (func.IsExtern)
                {  // foreign function names are not mangled
                    if (func.Name.StartsWith("GF_") || func.Name.StartsWith("GV_") || func.Name.StartsWith("T_") || func.Name.StartsWith("F_"))
                        Console.WriteLine("warning: extern function name could collide with mangled name");
                    _out.Write($"extern {PrintTy(ftype.Ret)} {func.Name}(");
                } else { 
                    _out.Write($"{PrintTy(ftype.Ret)} {NameMangle.GlobalName(func)}(");
                }
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
            _out.Write($"{PrintTy(ftype.Ret)} {NameMangle.GlobalName(func)}(");
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
                    string outPrefix = "";
                    string savedEnvName = "";
                    if (!callInstr.Out.IsNull) outPrefix = $"{Declare(callInstr.Out)} = ";
                    if (callInstr.IsDirect)
                    {
                        // just invoke directly the global function
                        // except (!) extern function names are unmangled
                        if (callInstr.Callee.GetGlobalValue().IsExtern) _out.Write(outPrefix + callInstr.Callee.GetGlobalValue().Name + "(");
                        else _out.Write(outPrefix + NameMangle.GlobalName(callInstr.Callee.GetGlobalValue()) + "(");
                    } else
                    {
                        // invoke a closure
                        savedEnvName = "saved_env" + Rnd();
                        _out.WriteLine($"void* {savedEnvName} = CLOENV;");
                        _out.WriteLine($"CLOENV = {Print(callInstr.Callee)}.env;");
                        _out.Write($"{outPrefix}({Print(callInstr.Callee)}.fptr)(");
                    }
                    for (int i = 0; i < callInstr.Arguments.Length; i++)
                    {
                        if (i != 0) _out.Write(", ");
                        _out.Write(Print(callInstr.Arguments[i]));
                    }
                    _out.WriteLine(");");
                    // restore cloenv
                    if (!callInstr.IsDirect)
                        _out.WriteLine($"CLOENV = {savedEnvName};");
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
                    _out.WriteLine($" ((I32){Print(icmpInstr.Rhs)});");
                    break;
                case MidINegInstr iNegInstr:
                    _out.WriteLine($"{Declare(iNegInstr.Out)} = -((I32){Print(iNegInstr.Val)});");
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
                case MidConstStringInstr stringInstr:
                    {
                        var bytes = Encoding.UTF8.GetBytes(stringInstr.Text);
                        var strLen = bytes.Length;
                        _out.Write(Declare(stringInstr.Out) + " = \"");
                        _out.Write($"\\x{strLen & 0xFF:X2}");
                        _out.Write($"\\x{(strLen & 0xFF00) >> 8:X2}");
                        _out.Write($"\\x{(strLen & 0xFF0000) >> 16:X2}");
                        _out.Write($"\\x{(strLen & 0xFF000000) >> 24:X2}");
                        foreach (var b in bytes)
                            _out.Write($"\\x{(int)b:X2}");
                        _out.WriteLine("\";");
                    }
                    break;
                case MidFailInstr failInstr:
                    _out.WriteLine($"__rtfail(\"{failInstr.FailText}\");");
                    break;
                case MidBitcastInstr bitcastInstr:
                    _out.WriteLine(Declare(bitcastInstr.Out) + $" = (({PrintTy(bitcastInstr.TargetType)}) {Print(bitcastInstr.Val)});");
                    break;
                case MidAndInstr andInstr:
                    _out.WriteLine($"{Declare(andInstr.Out)} = ((bool){Print(andInstr.Lhs)}) && ((bool){Print(andInstr.Rhs)});");
                    break;
                case MidOrInstr orInstr:
                    _out.WriteLine($"{Declare(orInstr.Out)} = ((bool){Print(orInstr.Lhs)}) || ((bool){Print(orInstr.Rhs)});");
                    break;
                case MidNotInstr notInstr:
                    _out.WriteLine($"{Declare(notInstr.Out)} = !((bool){Print(notInstr.Val)});");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private string Rnd() => Random.Shared.Next(1000, 10000).ToString();

        private string Declare(MidValue val)
            => PrintTy(val.GetType()) + " " + Print(val);

        private string Print(MidValue val)
        {
            if (val.IsConstInt) return $"((I32){val.GetIntValue()})";
            if (val.IsConstBool) return val.GetBoolValue() ? "true" : "false";
            if (val.IsConstChar) return $"((CHAR){(int)val.GetCharValue()})";
            if (val.IsReg) return NameMangle.LocalName(val);
            if (val.IsGlobal)
            {
                // If global values = global functions are used directly as values,
                // the "closure value" must be returned instead (not just the direct function pointer)
                return $"(({PrintTy(val.GetGlobalValue().FuncType)}){{{NameMangle.GlobalName(val.GetGlobalValue())}, null}})";
            }
            throw new NotSupportedException();
        }

        private string PrintTy(TypeRef ty) => NameMangle.NameType(ty);
    }
}
