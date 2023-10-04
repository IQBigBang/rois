using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.utils
{
    public struct SourcePos
    {
        public readonly int Line;
        public readonly int Column;

        public SourcePos(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public static readonly SourcePos None = new SourcePos(-1, -1);
    }

    [Serializable]
    public class CompilerError : Exception
    {
        public string Text;
        public SourcePos Pos;

        public CompilerError(string text, SourcePos? pos = null)
        {
            Text = text;
            Pos = pos ?? SourcePos.None;
        }

        public override string ToString() => Text;
    }
}
