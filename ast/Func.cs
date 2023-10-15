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

    public abstract class UserTypeDef
    {
        public readonly string Name;
        public NamedType? Type;
        public SourcePos Pos;
        public Func[] Methods;

        protected UserTypeDef(string name, SourcePos pos, Func[] methods)
        {
            Name = name;
            Pos = pos;
            Methods = methods;
        }
    }

    public class ClassDef : UserTypeDef
    {
        public (TypeRef, string)[] Fields;

        public ClassDef(string name, (TypeRef, string)[] fields, Func[] methods, SourcePos pos)
            : base(name, pos, methods)
        {
            Fields = fields;
        }
    }

    public class EnumClassDef : UserTypeDef
    {
        public record Variant(string VariantName, (TypeRef, string)[] Fields, SourcePos Pos);

        public Variant[] Variants;

        public EnumClassDef(string name, Variant[] variants, Func[] methods, SourcePos pos)
            : base(name, pos, methods)
        {
            Variants = variants;
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
