using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.utils
{
    public struct SourcePos
    {
        /// <summary>
        /// One-based line number
        /// </summary>
        public readonly int Line;
        /// <summary>
        /// One-based column number
        /// </summary>
        public readonly int Column;

        public SourcePos(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public static readonly SourcePos Zero = new SourcePos(0, 0);
    }

    public class CompilerError : Exception
    {
        public enum Type
        {
            ParseError,
            TypeError,
            NameError,
            ValidationError,
            OtherError,
        };

        public Type Typ;
        public string Desc;
        public SourcePos Pos;

        public CompilerError(Type type, SourcePos pos, string desc = "")
        {
            Typ = type;
            Desc = desc;
            Pos = pos;
        }

        public static CompilerError ParseErr(string desc, SourcePos pos) => new(Type.ParseError, pos, desc);
        public static CompilerError TypeErr(string desc, SourcePos pos) => new(Type.TypeError, pos, desc);
        public static CompilerError NameErr(string desc, SourcePos pos) => new (Type.NameError, pos, desc);
        public static CompilerError ValidationErr(string desc, SourcePos pos) => new(Type.ValidationError, pos, desc);
        public static CompilerError OtherErr(string desc, SourcePos pos) => new(Type.OtherError, pos, desc);

        public override string ToString()
        {
            string prefix = Typ switch
            {
                Type.ParseError => "Parsing error",
                Type.TypeError => "Typing error",
                Type.NameError => "Naming error",
                Type.ValidationError => "Validation error",
                Type.OtherError => "Error"
            };
            prefix = "\x1b[31m" + prefix + "\x1b[0m"; // red color
            prefix += $" at line {Pos.Line}, column {Pos.Column}";
            if (Desc != "")
                return prefix + ": " + Desc;
            else return prefix;
        }

        public string AsJson()
        {
            string messageWithoutPos = Typ switch
            {
                Type.ParseError => "Parsing error",
                Type.TypeError => "Typing error",
                Type.NameError => "Naming error",
                Type.ValidationError => "Validation error",
                Type.OtherError => "Error"
            };
            messageWithoutPos += ": " + Desc;
            return "{\"type\": \"error\"," +
                   $"\"where\": \"{Pos.Line}:{Pos.Column}\"," +
                   $"\"message\": \"{messageWithoutPos}\"}}";
        }
    }
}
