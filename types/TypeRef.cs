using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public abstract class TypeRef
    {
        public bool IsVoid => this is VoidType;
        public bool IsInt => this is IntType;
        public bool IsFunc => this is FuncType;
        public bool IsBool => this is BoolType;
        public bool IsStructClass => this is NamedType nt && nt.IsStructClass;
        public bool IsEnumClass => this is NamedType nt && nt.IsEnumClass;

        public static readonly TypeRef UNKNOWN = new TypeUnknown();
        public static readonly TypeRef INT = new IntType();
        public static readonly TypeRef VOID = new VoidType();
        public static readonly TypeRef BOOL = new BoolType();
        public static readonly TypeRef PTR = new PtrType();
        public static readonly TypeRef CHAR = new CharType();

        public override bool Equals(object? obj)
        {
            if (obj is TypeRef tr) return Equal(tr);
            return false;
        }

        public bool Equal(TypeRef other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (IsVoid && other.IsVoid) return true;
            if (IsInt && other.IsInt) return true;
            if (IsBool && other.IsBool) return true;
            if (this is PtrType && other is PtrType) return true;
            if (this is CharType && other is CharType) return true;
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
            if (this is NamedType nt1 && other is NamedType nt2)
                return nt1.Name == nt2.Name;
            return false;
        }
    }

    public class TypeUnknown : TypeRef
    {
        public override string ToString() => "typUnk";
    }
}
