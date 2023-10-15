using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    public class MidModule
    {
        public List<MidFunc> Functions;
        public List<NamedType> Classes;

        public MidModule(List<MidFunc> functions, List<NamedType> classes)
        {
            Functions = functions;
            Classes = classes;
        }
    }
}
