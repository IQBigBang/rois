// See https://aka.ms/new-console-template for more information
using RoisLang.asm;
using RoisLang.lower;
using RoisLang.parser;
using RoisLang.types;

string testWS =
@"
    
        
    

";
string test =
@"let foo = 22
let Bar = 78
Bar = foo
foo = 855
let c = foo";

string test3 =
@"def main(a: int, b: int) -> int:
    return (a + b) - (b + a)
";

string test4 =
@"def f():
    let a = g

def g():
    let a = f
";

//var tokens = Lexer.TokenizeString(test3);
var program = Parser.LexAndParse(test4);
new TypeChecker().TypeckProgram(program);
var lowerer = new AstLowerer();
var midFuncs = lowerer.LowerProgram(program);
midFuncs.ForEach(x => x.Dump());
Console.WriteLine();
/*foreach (var stmt in parseResult)
    lowerer.LowerStmt(stmt);*/
//new RegAlloc().RegAllocBlock(lowerer.GetBlock());
//var output = File.Open("output.nasm", FileMode.Create);
AsmCompile.CompileAllFuncs(Console.Out, midFuncs);
//output.Close();