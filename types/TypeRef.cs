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
    }
}
