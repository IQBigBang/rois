using RoisLang.mid_ir;
using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm.c
{
    /// <summary>
    /// The mangling scheme is as follows:
    /// 
    /// All symbols are split into categories based on their first letter:
    /// * G... = global symbols
    /// * L... = local variables (correspond to registers in IR)
    /// * T... = user-defined types
    /// * F... = function types
    /// 
    /// 1. Local variables
    /// Because they are local, no specific name mangling scheme has to be defined.
    /// This is left mostly as an implementation detail.
    /// 
    /// 2. Global symbols
    /// Global symbols are all of the format "modules.name" where modules is a list of identifiers separated by '.'
    /// Global symbols are split into functions, which start with GF and other static variables which start with GV.
    /// Global functions are mangled as: "GF_{mangled_namespaced_name}_{function_args}_{function_ret}"
    /// Global variables are mangled as: "GV_{mangled_namespaced_name}_{type}"
    /// Methods are mangled as: "GM{mangled_namespaced_class_name}_{methodName}_{function_args}_{function_ret}"
    /// 
    /// 3. User-defined types
    /// User-defined types are mangled as: "T_{mangled_namespaced_name}" (similar to global symbols)
    /// 
    /// 4. Type mangling
    /// If a type needs to be mangled, the following scheme is used:
    /// * int32 = I
    /// * bool = B
    /// * void = V
    /// * ptr = P
    /// * func = F{argcount}_{args}_{ret}
    /// * class = C{mangled_namespaced_name}
    /// 
    /// 4.1 Argument mangling
    /// If multiple types following each other need to be mangled, it is done by splitting
    /// them with '_' and prefixing types longer than one character with their length. For example:
    /// fun(int) -> int = F1_I_I
    /// fun(MyClass) -> int = F1_8CMyClass_I
    /// fun(fun(int)) -> int = F1_6F1_I_V_I
    /// 
    /// 5. Namespaced name mangling
    /// If a name has no module, it's simply the name. (TODO!)
    /// </summary>
    internal class NameMangle
    {
        public static string LocalName(MidValue val)
            => $"L_{val.GetBasicBlock()}_{val.GetRegNum()}";

        public static string GlobalName(MidValue val)
            => GlobalName(val.GetGlobalValue());

        public static string GlobalName(MidFunc func)
        {
            string s;
            if (func.IsMethod)
            {
                var arr = func.Name.Split('$');
                s = "GM" + arr[0] + "_" + arr[1];
            }
            else
                s = "GF_" + NamespacedName(func.Name);
            foreach (var arg in ((FuncType)func.FuncType).Args)
            {
                var mangledArg = MangleTypeAsPartOfName(arg);
                if (mangledArg.Length > 1) s += "_" + mangledArg.Length + mangledArg;
                else s += "_" + mangledArg;
            }
            s += "_" + MangleTypeAsPartOfName(((FuncType)func.FuncType).Ret);
            return s;
        }

        public static string NameType(TypeRef tr)
            => tr switch
            {
                IntType => "I32",
                BoolType => "bool",
                VoidType => "void", // TODO
                PtrType => "PTR",
                ClassType cls => $"T_{NamespacedName(cls.Name)}",
                FuncType ft => MangleTypeAsPartOfName(ft), // the scheme is identical for function types (on purpose)
            };

        private static string NamespacedName(string s)
        {
            Debug.Assert(!s.Contains('.'));
            return s;
        }

        private static string MangleTypeAsPartOfName(TypeRef tr)
        {
            if (tr.IsVoid) return "V";
            if (tr.IsInt) return "I";
            if (tr.IsBool) return "B";
            if (tr is PtrType) return "P";
            if (tr.IsFunc) {
                var ftype = (FuncType)tr;
                string s = "F" + ftype.Args.Count;
                foreach (var arg in ftype.Args)
                {
                    var mangledArg = MangleTypeAsPartOfName(arg);
                    if (mangledArg.Length > 1) s += "_" + mangledArg.Length + mangledArg;
                    else s += "_" + mangledArg;
                }
                s += "_" + MangleTypeAsPartOfName(ftype.Ret);
                return s;
            }
            if (tr.IsClass)
                return "C" + NamespacedName(((ClassType)tr).Name);
            throw new NotImplementedException();
        }
    }
}
