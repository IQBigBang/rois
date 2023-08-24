using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    public abstract class MidInstr
    {
        // Required by the builder
        public abstract void SetOut(MidValue val);
        public abstract MidValue[] AllArgs();
        public abstract void Map(Func<MidValue, MidValue> map);

        public abstract void Dump();

        /// <summary>
        /// Replace all references to a certain register
        /// </summary>
        public void Replace(MidValue from, MidValue to) => Map((x) => x == from ? to : x);
    }

    /*/// <summary>
    /// `reg = Set val`
    /// Simply sets a register to be equal to a certain value
    /// </summary>
    public class MidSetInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Value;

        public override void SetOut(MidValue val) => Out = val;
        public override MidValue[] AllArgs() => new MidValue[] { Value };
        public override void Map(Func<MidValue, MidValue> map)
        {
            Out = map(Out);
            Value = map(Value);
        }

        public override void Dump()
        {
            Console.WriteLine($"{Out} = Set {Value}");
        }
    }*/

    public class MidIAddInstr : MidInstr
    {
        public MidValue Out;
        public MidValue Lhs;
        public MidValue Rhs;

        public override void SetOut(MidValue val) => Out = val;
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
}
