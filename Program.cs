// See https://aka.ms/new-console-template for more information
using RoisLang.asm;
using RoisLang.asm.c;
using RoisLang.lower;
using RoisLang.opt;
using RoisLang.mid_ir;
using RoisLang.parser;
using RoisLang.types;
using RoisLang.utils;

public static class Program
{
    public static int Main(string[] cmdArgs)
    {
        if (cmdArgs.Length < 1)
        {
            Console.Error.WriteLine("No input specified");
            return 1;
        }
        var inputPath = cmdArgs[0];
        if (cmdArgs.Length < 3 || cmdArgs[1] != "-o")
        {
            Console.Error.WriteLine("No output flag specified");
            return 1;
        }
        var outputPath = cmdArgs[2];

        /*var includeDirs = 
        foreach (var arg in cmdArgs)
            if (arg.StartsWith("-I"))*/

        bool verbose = cmdArgs.Contains("-v");
        bool dryRun = cmdArgs.Contains("-d");
        bool reportErrorsAsJson = cmdArgs.Contains("-json-errors");

        try
        {
            var program = MultiParser.Parse(inputPath);
            new TypeChecker().TypeckProgram(program);
            MatchLowerer.VisitProgram(program);
            var lowerer = new AstLowerer();
            var midFuncs = lowerer.LowerProgram(program);
            // opt passes
            midFuncs.Functions.ForEach(x => ((IPass)new ConstantFold()).RunOnFunction(x));
            midFuncs.Functions.ForEach(x => new RemoveDeadCode().RunOnFunction(x));
            if (verbose) midFuncs.Functions.ForEach(x => x.Dump());
            Console.WriteLine();
            if (dryRun) return 0;
            var output = File.Open(outputPath, FileMode.Create);
            new CCompile(new StreamWriter(output, System.Text.Encoding.UTF8)).CompileModule(midFuncs);
            output.Close();
            return 0;
        }
        catch (CompilerError cerr)
        {
            if (reportErrorsAsJson)
                Console.Error.WriteLine(cerr.AsJson());
            else
                Console.Error.WriteLine(cerr.ToString());
            return 107; // 107 means graceful failure
        }
    }
}