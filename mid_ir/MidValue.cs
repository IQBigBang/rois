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
    /// 
    /// MidValue's should generally speaking NOT be manually constructed,
    /// because they rely on POINTER equality
    /// </summary>
    public class MidValue
    {
        // 0 = undefined, 1 = const value, 2 = local virtual register, 3 = global value
        private readonly int tag;
        // if tag==1  => IConstValue
        //    tag==2  => Integer - the register number
        //    tag==3  => MidFunc - a global value
        private readonly object value;
        // if it's a local virtual register
        private readonly uint blockId;
        private readonly TypeRef ty;

        private MidValue(int tag, object value, TypeRef ty, uint blockId = 0)
        {
            this.tag = tag;
            this.blockId = blockId;
            this.value = value;
            this.ty = ty;
        }

        //public static MidValue Null = new MidValue(0, 0, TypeRef.UNKNOWN);
        public static MidValue Null() => new(0, 0, TypeRef.UNKNOWN);
        public static MidValue ConstInt(int val) => new(1, new IntValue(val), TypeRef.INT);
        public static MidValue ConstBool(bool val) => new(1, new BoolValue(val), TypeRef.BOOL);
        /// <summary>
        /// Every register value SHOULD be a singleton!
        /// </summary>
        internal static MidValue Reg(uint reg, uint blockId, TypeRef ty, Assertion this_is_a_singleton) => new(2, reg, ty, blockId);
        /// <summary>
        /// Every global value SHOULD be a singleton!
        /// </summary>
        internal static MidValue Global(MidFunc func, Assertion this_is_a_singleton) => new(3, func, func.FuncType);

        public bool IsNull => tag == 0;
        public bool IsConst => tag == 1;
        public bool IsConstInt => IsConst && value is IntValue;
        public bool IsConstBool => IsConst && value is BoolValue;
        public bool IsReg => tag == 2;
        public bool IsGlobal => tag == 3;
        
        public uint GetBasicBlock()
        {
            if (IsReg) return blockId;
            else throw new InvalidOperationException();
        }

        public uint GetRegNum()
        {
            if (IsReg) return (uint)value;
            else throw new InvalidOperationException();
        }

        public int GetIntValue()
        {
            if (IsConstInt) return ((IntValue)value).Value;
            else throw new InvalidOperationException();
        }

        public bool GetBoolValue()
        {
            if (IsConstBool) return ((BoolValue)value).Value;
            else throw new InvalidOperationException();
        }

        public MidFunc GetGlobalValue()
        {
            if (IsGlobal) return (MidFunc)value;
            else throw new InvalidOperationException();
        }

        public override bool Equals(object? obj)
        {
            if (obj is MidValue other)
            {
                if (ReferenceEquals(this, other)) return true;
                if (tag != other.tag) return false;
                if (IsNull) return true;
                if (IsConstInt)
                    return GetIntValue() == other.GetIntValue();
                if (IsConstBool)
                    return GetBoolValue() == other.GetBoolValue();
                if (IsReg)
                    // TODO: should we check types for equality (?)
                    return GetRegNum() == other.GetRegNum() && GetBasicBlock() == other.GetBasicBlock();
                if (IsGlobal)
                {
                    // there's no guaranteed way to test `MidFunc`s for equality, so we rely on references and name equality
                    return ReferenceEquals(GetGlobalValue(), other.GetGlobalValue())
                           || GetGlobalValue().Name == other.GetGlobalValue().Name;
                }
            }
            return false;
        }
        public override int GetHashCode() => HashCode.Combine(tag, value, blockId);

        public static bool operator ==(MidValue left, MidValue right) => left.Equals(right);

        public static bool operator !=(MidValue left, MidValue right) => !left.Equals(right);

        public void Dump() => Console.Write(ToString());
        public override string ToString()
        {
            if (tag == 0) return "undefined";
            if (tag == 1) return "const " + value;
            if (tag == 2) return ty + " %" + value;
            if (tag == 3) return "@" + GetGlobalValue().Name;
            return "!INVALID_VALUE";
        }

        public void AssertType(TypeRef typ)
        {
            if (!ty.Equal(typ)) throw new Exception("Wrong type of MidValue");
        }

        public new TypeRef GetType() => ty;
    }

    /// <summary>
    /// The purpose of the `Assertion` type
    /// is to alarm the programmer to pay special attention to a certain invariant
    /// </summary>
    internal struct Assertion {
        public Assertion() {}
        public static Assertion X = new();
    }

    public interface IConstValue { }
    public struct IntValue : IConstValue
    {
        public int Value;
        public IntValue(int value)
        {
            Value = value;
        }
        public override string ToString() => Value.ToString();
    }
    public struct BoolValue : IConstValue
    {
        public bool Value;
        public BoolValue(bool value)
        {
            Value = value;
        }
        public override string ToString() => Value ? "true" : "false";
    }
}
