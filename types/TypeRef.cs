using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public abstract class TypeRef
    {
        public abstract bool IsVoid { get; }
        public abstract bool IsInt { get; }
        public abstract bool IsFunc { get; }

        public static readonly TypeRef UNKNOWN = new TypeUnknown();
        public static readonly TypeRef INT = new IntType();
        public static readonly TypeRef VOID = new VoidType();

        public override bool Equals(object? obj)
        {
            if (obj is TypeRef tr) return Equal(tr);
            return false;
        }

        public bool Equal(TypeRef other)
        {
            if (IsVoid && other.IsVoid) return true;
            if (IsInt && other.IsInt) return true;
            if (IsFunc && other.IsFunc)
            {
                var this_ = (FuncType)this;
                var other_ = (FuncType)other;
                if (!this_.Ret.Equal(other_.Ret)) return false;
                if (this_.Args.Count != other_.Args.Count) return false;
                for (int i = 0; i < this_.Args.Count; i++)
                {
                    if (!this_.Args[i].Equal(other_.Args[i])) return false;
                }
                return true;
            }
            return false;
        }
    }

    public class TypeUnknown : TypeRef
    {
        public override bool IsVoid => false;
        public override bool IsInt => false;
        public override bool IsFunc => false;

        public override string ToString() => "typUnk";
    }
}
