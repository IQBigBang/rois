using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class ClassType : TypeRef
    {
        public string Name;
        public (string, TypeRef)[] Fields;

        public ClassType(string name)
        {
            Name = name;
            Fields = Array.Empty<(string, TypeRef)>();
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override bool IsVoid => false;
        public override bool IsInt => false;
        public override bool IsFunc => false;

        public override string ToString() => "$" + Name;
    }
}
