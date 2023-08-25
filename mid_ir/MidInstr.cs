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
        public abstract TypeRef OutType();
        public abstract MidValue[] AllArgs();
        public abstract void Map(Func<MidValue, MidValue> map);

        public abstract void Dump();

        /// <summary>
        /// Replace all references to a certain register
        /// </summary>
        public void Replace(MidValue from, MidValue to) => Map((x) => x == from ? to : x);
    }

    public class MidIAddInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;

        public override bool HasOut() => true;
        public override void SetOut(MidValue val) => Out = val;
        public override TypeRef OutType() => TypeRef.INT;
        public override MidValue[] AllArgs() => new MidValue[] { Out, Lhs, Rhs };
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

    public class MidIRet : MidInstr
    {
        // This may be null, which means a "void value" is returned
        public MidValue Value;

        public override MidValue[] AllArgs() => new MidValue[] { Value };

        public override void Dump()
        {
            if (Value.IsNull)
                Console.WriteLine("Ret");
            else
                Console.WriteLine($"Ret {Value}");
        }

        public override bool HasOut() => false;
        public override TypeRef OutType() => TypeRef.VOID;
        public override void SetOut(MidValue val) { }

        public override void Map(Func<MidValue, MidValue> map)
        {
            Value = map(Value);
        }


    }
}
