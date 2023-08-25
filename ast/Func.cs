using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.ast
{
    public class Func
    {
        public string Name;
        public (string, TypeRef)[] Arguments;
        public TypeRef Ret;
        public Stmt[] Body;

        public Func(string name, (string, TypeRef)[] arguments, Stmt[] body, TypeRef ret)
        {
            Name = name;
            Arguments = arguments;
            Body = body;
            Ret = ret;
        }
    }
}
