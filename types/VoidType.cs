using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class VoidType : TypeRef
    {
        public override bool IsVoid => true;
        public override bool IsInt => false;
    }
}
