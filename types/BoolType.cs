using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    internal class BoolType : TypeRef
    {
        public override bool IsVoid => false;
        public override bool IsInt => false;
        public override bool IsFunc => false;

        public override string ToString() => "bool";
    }
}
