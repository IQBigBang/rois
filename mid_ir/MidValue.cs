using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    /// <summary>
    /// Value which can be used as an operand in the MidIR.
    /// Either a:
    /// 1) virtual register (local)
    /// 2) constant value (of type int)
    /// 3) global value
    /// </summary>
    public struct MidValue
    {
        private TypeRef ty;
        // 0 = undefined, 1 = const int, 2 = local virtual register
        private ushort tag;
        private ushort extra;
        private uint value;

        private MidValue(ushort tag, uint value, TypeRef ty, ushort extra = 0)
        {
            this.tag = tag;
            this.extra = extra;
            this.value = value;
            this.ty = ty;
        }

        //public static MidValue Null = new MidValue(0, 0, TypeRef.UNKNOWN);
        public static MidValue Null() => new(0, 0, TypeRef.UNKNOWN);
        public static MidValue ConstInt(int val) => new(1, (uint)val, TypeRef.INT);
        public static MidValue Reg(uint reg, uint blockId, TypeRef ty) => new(2, reg, ty, (ushort)blockId);

        public bool IsNull => tag == 0;
        public bool IsConstInt => tag == 1;
        public bool IsReg => tag == 2;
        
        public int GetBasicBlock()
        {
            if (IsReg) return extra;
            return -1;
        }

        public int GetRegNum()
        {
            if (IsReg) return (int)value;
            return int.MaxValue;
        }

        public override bool Equals(object? obj)
        {
            // TODO: should we check types for equality (?)
            return (obj is MidValue other && other.tag == tag && other.value == value && other.extra == extra);
        }
        public override int GetHashCode() => HashCode.Combine(tag, value, extra);

        public static bool operator ==(MidValue left, MidValue right) => left.Equals(right);

        public static bool operator !=(MidValue left, MidValue right) => !left.Equals(right);

        public void Dump() => Console.Write(this.ToString());
        public override string ToString()
        {
            if (tag == 0) return "undefined";
            if (tag == 1) return "const " + value;
            if (tag == 2) return ty + " %" + value;
            return "!INVALID_VALUE";
        }

        public void AssertType(TypeRef typ)
        {
            if (!ty.Equal(typ)) throw new Exception("");
        }
    }
}
