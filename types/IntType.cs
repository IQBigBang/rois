using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class IntType : TypeRef
    {
        public override bool IsVoid => false;
        public override bool IsInt => true;
        public override bool IsFunc => false;

        public override string ToString() => "int";
    }
}
