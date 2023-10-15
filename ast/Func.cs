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

    public interface UserTypeDef
    {
        public string Name { get; }
        public TypeRef? Type { get; set; }
        public SourcePos Pos { get; }
    }

    public class ClassDef : UserTypeDef
    {
        public readonly string Name;
        string UserTypeDef.Name => Name;
        public (TypeRef, string)[] Fields;
        public NamedType? Type;
        TypeRef? UserTypeDef.Type { get => Type; set => Type = (NamedType)value!; }
        public Func[] Methods;
        public SourcePos Pos;
        SourcePos UserTypeDef.Pos => Pos;

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
        public UserTypeDef[] UserTypes;
        public Func[] Functions;

        public Program(UserTypeDef[] classes, Func[] functions)
        {
            UserTypes = classes;
            Functions = functions;
        }
    }
}
