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

        public (TypeRef, string)[] Fields => (Def as ast.ClassDef)!.Fields;

        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => "$" + Name;
    }
}
