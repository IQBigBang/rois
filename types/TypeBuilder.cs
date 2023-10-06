using RoisLang.ast;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.types
{
    internal class TypeBuilder
    {
        private readonly Dictionary<string, (ClassType, bool)> classes;

        public TypeBuilder() 
        {
            classes = new();
        }

        public ClassType GetClassType(string name)
        {
            if (classes.TryGetValue(name, out (ClassType, bool) result))
                return result.Item1;
            var k = new ClassType(name);
            classes.Add(name, (k, false));
            return k;
        }

        public void InitializeAll(IEnumerable<ClassDef> classDefs)
        {
            foreach (var cls in classDefs)
            {
                GetClassType(cls.Name); // this ensures that the class exists in the list
                var k = classes[cls.Name];
                if (k.Item2 == true) throw CompilerError.NameErr($"Class `{cls.Name}` defined twice", cls.Pos);
                k.Item1.Fields = cls.Fields.Select(x => (x.Item2, x.Item1)).ToArray();
                classes[cls.Name] = (k.Item1, true);
                cls.Type = k.Item1;
            }

            foreach (var k in classes)
                if (k.Value.Item2 == false)
                    throw CompilerError.NameErr($"Class `{k.Value.Item1}` not defined", SourcePos.Zero);
        }
    }
}
