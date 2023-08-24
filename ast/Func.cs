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
        public string[] Arguments;
        public Stmt[] Body;

        public Func(string name, string[] arguments, Stmt[] body)
        {
            Name = name;
            Arguments = arguments;
            Body = body;
        }
    }
}
