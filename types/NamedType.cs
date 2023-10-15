using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class NamedType : TypeRef
    {
        public string Name;
        public ast.UserTypeDef? Def;

        public NamedType(string name)
        {
            Name = name;
        }

        public bool IsStructClass => Def is ast.ClassDef;
        public bool IsEnumClass => Def is ast.EnumClassDef;

        public (TypeRef, string)[] Fields => (Def as ast.ClassDef)!.Fields;
        public ast.EnumClassDef.Variant[] Variants => (Def as ast.EnumClassDef)!.Variants;

        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => "$" + Name;
    }
}
