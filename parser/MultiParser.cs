using RoisLang.types;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.parser
{
    internal class MultiParser
    {
        private types.TypeBuilder typeBuilder;
        private HashSet<string> includedFiles = new (); // to prevent duplication
        private ast.Program oneBigProgram = new (Array.Empty<ast.ClassDef>(), Array.Empty<ast.Func>());

        private MultiParser(TypeBuilder typeBuilder)
        {
            this.typeBuilder = typeBuilder;
        }

        public static ast.Program Parse(string filePath)
        {
            var mp = new MultiParser(new TypeBuilder());
            mp.ParseFile(Path.GetFullPath(filePath));
            mp.typeBuilder.InitializeAll(mp.oneBigProgram.Classes);
            return mp.oneBigProgram;
        }

        private void ParseFile(string filePath)
        {
            if (includedFiles.Contains(filePath))
                return;
            includedFiles.Add(filePath);
            var source = File.ReadAllText(filePath, Encoding.UTF8);
            var (includes, thisProgram) = new Parser(typeBuilder).LexAndParse(source);
            MergeProgram(thisProgram);
            foreach (var include in includes)
                TryInclude(include, filePath);
        }

        private void TryInclude(string includeName, string origFilePath)
        {
            var directory = Path.GetDirectoryName(origFilePath);
            var attempt1 = Path.Combine(directory!, includeName + ".ro");
            if (File.Exists(attempt1))
            {
                ParseFile(attempt1);
                return;
            }
            var attempt2 = Path.Combine(directory!, includeName.ToLower() + ".ro");
            if (File.Exists(attempt2))
            {
                ParseFile(attempt2);
                return;
            }
            throw CompilerError.OtherErr($"Couldn't find included file `{includeName}`", SourcePos.Zero);
        }

        void MergeProgram(ast.Program pr)
        {
            oneBigProgram.Classes = pr.Classes.Concat(oneBigProgram.Classes).ToArray();
            oneBigProgram.Functions = pr.Functions.Concat(oneBigProgram.Functions).ToArray();
        }
    }
}
