using RoisLang.types;
using RoisLang.utils;
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
        /// <summary>
        /// Extern functions don't have a Body
        /// </summary>
        public bool Extern;
        public (string, TypeRef)[] Arguments;
        public TypeRef Ret;
        public Stmt[] Body;
        public SourcePos Pos;

        public Func(string name, (string, TypeRef)[] arguments, Stmt[] body, TypeRef ret, SourcePos pos, bool @extern = false)
        {
            Name = name;
            Arguments = arguments;
            Body = body;
            Ret = ret;
            Extern = @extern;
            Pos = pos;
        }
    }

    public class ClassDef
    {
        public string Name;
        public (TypeRef, string)[] Fields;
        public ClassType? Type;
        public Func[] Methods;
        public SourcePos Pos;

        public ClassDef(string name, (TypeRef, string)[] fields, Func[] methods, SourcePos pos)
        {
            Name = name;
            Fields = fields;
            Methods = methods;
            Pos = pos;
        }
    }

    public class Program
    {
        public ClassDef[] Classes;
        public Func[] Functions;

        public Program(ClassDef[] classes, Func[] functions)
        {
            Classes = classes;
            Functions = functions;
        }
    }
}
