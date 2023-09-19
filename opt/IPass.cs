using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.opt
{
    public interface IPass
    {
        public void RunOnFunction(MidFunc func)
        {
            foreach (var block in func.Blocks)
            {
                RunOnBlock(block);
            }
        }

        public abstract void RunOnBlock(MidBlock block);
    }
}
