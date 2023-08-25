using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class FuncType : TypeRef
    {
        public override bool IsVoid => false;
        public override bool IsInt => false;
        public override bool IsFunc => true;

        public readonly IReadOnlyList<TypeRef> Args;
        public readonly TypeRef Ret;

        public FuncType(IEnumerable<TypeRef> args, TypeRef ret)
        {
            Args = new List<TypeRef>(args);
            Ret = ret;
        }
    }
}
