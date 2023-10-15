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
        private readonly Dictionary<string, (NamedType, bool)> classes;

        public TypeBuilder() 
        {
            classes = new();
        }

        public NamedType GetNamedType(string name)
        {
            if (classes.TryGetValue(name, out (NamedType, bool) result))
                return result.Item1;
            var k = new NamedType(name);
            classes.Add(name, (k, false));
            return k;
        }

        public void InitializeAll(IEnumerable<UserTypeDef> userTypeDefs)
        {
            foreach (var cls in userTypeDefs)
            {
                GetNamedType(cls.Name); // this ensures that the class exists in the list
                var k = classes[cls.Name];
                if (k.Item2 == true) throw CompilerError.NameErr($"Type `{cls.Name}` defined twice", cls.Pos);
                k.Item1.Def = cls;
                classes[cls.Name] = (k.Item1, true);
                cls.Type = k.Item1;
            }

            foreach (var k in classes)
                if (k.Value.Item2 == false)
                    throw CompilerError.NameErr($"Type `{k.Value.Item1}` not defined", SourcePos.Zero);
        }
    }
}
