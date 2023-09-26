using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    public abstract class MidInstr
    {
        /// <summary>
        /// Does the instruction have a meaningful output? 
        /// (For example, calls to void functions or `ret` do not)
        /// </summary>
        /// <returns></returns>
        public abstract bool HasOut();
        // Required by the builder
        public abstract void SetOut(MidValue val);
        public abstract MidValue GetOut();
        public abstract TypeRef OutType();
        public abstract IEnumerable<MidValue> AllArgs();
        public abstract void Map(Func<MidValue, MidValue> map);

        public abstract void Dump();

        /// <summary>
        /// Replace all references to a certain register
        /// </summary>
        public void Replace(MidValue from, MidValue to) => Map((x) => x == from ? to : x);

        /// <summary>
        /// If this is true, the `LiveRegData` is inserted when register allocation happens
        /// </summary>
        public virtual bool RequiresLiveRegData => false;
        public InstrExtraData? extra;
    }

    public abstract class InstrExtraData { }
    /*public class LiveRegData : InstrExtraData
    {
        /// <summary>
        /// Which registers are live and need to be preserved through this instruction.
        /// This information is important for e.g. `call` instructions
        /// </summary>
        public readonly List<asm.GpReg> LiveRegisters;
        public LiveRegData(List<asm.GpReg> liveRegisters) {  LiveRegisters = liveRegisters; }
    }*/

    public class MidIAddInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => TypeRef.INT;
        public override IEnumerable<MidValue> AllArgs() { yield return Out; yield return Lhs; yield return Rhs; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Lhs = map(Lhs);
            Rhs = map(Rhs);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = IAdd {Lhs}, {Rhs}");
        }
    }

    public class MidISubInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => TypeRef.INT;
        public override IEnumerable<MidValue> AllArgs() { yield return Out; yield return Lhs; yield return Rhs; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Lhs = map(Lhs);
            Rhs = map(Rhs);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = ISub {Lhs}, {Rhs}");
        }
    }

    public class MidIMulInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => TypeRef.INT;
        public override IEnumerable<MidValue> AllArgs() { yield return Out; yield return Lhs; yield return Rhs; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Lhs = map(Lhs);
            Rhs = map(Rhs);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = IMul {Lhs}, {Rhs}");
        }
    }

    public class MidRetInstr : MidInstr
    {
        // This may be null, which means a "void value" is returned
        public MidValue Value;

        public override IEnumerable<MidValue> AllArgs() { yield return Value; }

        public override void Dump()
        {
            if (Value.IsNull)
                Console.WriteLine("Ret");
            else
                Console.WriteLine($"Ret {Value}");
        }

        public override bool HasOut() => false;
        public override MidValue GetOut() => MidValue.Null();
        public override TypeRef OutType() => TypeRef.VOID;
        public override void SetOut(MidValue val) { }

        public override void Map(Func<MidValue, MidValue> map)
        {
            Value = map(Value);
        }
    }

    public class MidCallInstr : MidInstr
    {
        // May be null, if the function returns void
        public MidValue Out;
        public MidValue Callee;
        public MidValue[] Arguments;

        public override bool HasOut() => !OutType().IsVoid;
        public override void SetOut(MidValue val) { if (HasOut()) Out = val; }
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => ((FuncType)Callee.GetType()).Ret;
        public override IEnumerable<MidValue> AllArgs()
        {
            yield return Out;
            yield return Callee;
            foreach (var a in Arguments) yield return a;
        }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Callee = map(Callee);
            Arguments = Arguments.Select(map).ToArray();
        }
        public override void Dump()
        {
            if (!Out.IsNull)
                Console.Write($"{Out} = ");
            Console.Write($"Call {Callee}");
            for (int i = 0; i < Arguments.Length; i++)
            {
                Console.Write($", {Arguments[i]}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// CallInstr performs a call to foreign code, it needs to know which registers to preserve
        /// </summary>
        public override bool RequiresLiveRegData => true;
    }

    public class MidICmpInstr : MidInstr
    {
        public enum CmpOp { Eq, NEq, Lt, Le, Gt, Ge }
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;
        public CmpOp Op;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => TypeRef.BOOL;
        public override IEnumerable<MidValue> AllArgs() { yield return Out; yield return Lhs; yield return Rhs; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Lhs = map(Lhs);
            Rhs = map(Rhs);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = ICmp.{Op} {Lhs}, {Rhs}");
        }
    }

    public class MidGotoInstr : MidInstr
    {
        public int TargetBlockId = -1;
        public MidValue[] Arguments;

        public override IEnumerable<MidValue> AllArgs() => Arguments;

        public override MidValue GetOut() => MidValue.Null();
        public override bool HasOut() => false;
        public override void SetOut(MidValue val) { }
        public override TypeRef OutType() => TypeRef.VOID;

        public override void Map(Func<MidValue, MidValue> map)
        {
            Arguments = Arguments.Select(map).ToArray();
        }

        public override void Dump()
        {
            Console.Write($"Goto BB{TargetBlockId}(");
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (i != 0) Console.Write(", "); ;
                Console.Write(Arguments[i]);
            }
            Console.WriteLine(")");
        }
    }

    public class MidBranchInstr : MidInstr
    {
        public MidValue Cond;
        public MidGotoInstr Then;
        public MidGotoInstr Else;

        public override IEnumerable<MidValue> AllArgs()
        {
            yield return Cond;
            foreach (var x in Then.AllArgs()) yield return x;
            foreach (var x in Else.AllArgs()) yield return x;
        }

        public override MidValue GetOut() => MidValue.Null();
        public override bool HasOut() => false;
        public override void SetOut(MidValue val) { }
        public override TypeRef OutType() => TypeRef.VOID;

        public override void Map(Func<MidValue, MidValue> map)
        {
            Cond = map(Cond);
            Then.Map(map);
            Else.Map(map);
        }

        public override void Dump()
        {
            Console.Write($"If {Cond} Then ");
            Then.Dump();
            Console.Write("      Else ");
            Else.Dump();
        }
    }

    public record FieldInfo(ClassType Class, int FieldN)
    {
        public FieldInfo(ClassType class_, string fieldName)
            : this(class_, Array.FindIndex(class_.Fields, x => x.Item1 == fieldName)) { }
        public override string ToString() => $"{Class}.{Class.Fields[FieldN].Item1}";
        public TypeRef FieldType() => Class.Fields[FieldN].Item2;
        public string FieldName() => Class.Fields[FieldN].Item1;
    }

    public class MidLoadInstr : MidInstr
    {
        public FieldInfo FieldInfo;
        public MidValue Object;
        public MidValue Out;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => FieldInfo.FieldType();
        public override IEnumerable<MidValue> AllArgs() { yield return Out; yield return Object; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Object = map(Object);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = Load {FieldInfo} {Object}");
        }
    }

    public class MidStoreInstr : MidInstr
    {
        public FieldInfo FieldInfo;
        public MidValue Object;
        public MidValue Value;

        public override bool HasOut() => false;
        public override void SetOut(MidValue val) { }
        public override MidValue GetOut() => MidValue.Null();
        public override TypeRef OutType() => TypeRef.VOID;
        public override IEnumerable<MidValue> AllArgs() { yield return Value; yield return Object; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Value = map(Value);
            Object = map(Object);
        }
        public override void Dump()
        {
            Console.WriteLine($"Store {FieldInfo} {Object}, {Value}");
        }
    }

    public class MidAllocClassInstr : MidInstr
    {
        public ClassType Class;
        public MidValue Out;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override MidValue GetOut() => Out;
        public override TypeRef OutType() => Class;
        public override IEnumerable<MidValue> AllArgs() { yield return Out; }
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
        }
        public override void Dump()
        {
            Console.WriteLine($"{Out} = AllocClass {Class}");
        }
    }
}
