using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    public class FuncType : TypeRef
    {
        // function types often repeat, "intern" them for more efficiency
        private static List<FuncType> _functions = new();

        public override bool IsVoid => false;
        public override bool IsInt => false;
        public override bool IsFunc => true;

        public readonly IReadOnlyList<TypeRef> Args;
        public readonly TypeRef Ret;

        protected FuncType(IEnumerable<TypeRef> args, TypeRef ret)
        {
            Args = new List<TypeRef>(args);
            Ret = ret;
        }

        public static FuncType New(List<TypeRef> args, TypeRef ret)
        {
            var f = _functions.Find(ft => FnCompare(ft.Args, ft.Ret, args, ret));
            if (f is not null)
                return f;
            else
            {
                var instance = new FuncType(args, ret);
                _functions.Add(instance);
                return instance;
            }
        }

        private static bool FnCompare(IReadOnlyList<TypeRef> args1, TypeRef ret1, IReadOnlyList<TypeRef> args2, TypeRef ret2)
        {
            if (!ret1.Equal(ret2)) return false;
            if (args1.Count != args2.Count) return false;
            for (int i = 0; i < args1.Count; i++)
            {
                if (!args1[i].Equal(args2[i])) return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "fn(" + string.Join(',', Args) + ") -> " + Ret;
        }

        public static IEnumerable<FuncType> AllFuncTypes => _functions;
    }
}
