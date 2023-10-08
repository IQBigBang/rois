using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class IntType : TypeRef
    {
        public override string ToString() => "int";
    }

    public class CharType : TypeRef
    {
        public override string ToString() => "char";
    }
}
