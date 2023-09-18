// See https://aka.ms/new-console-template for more information
using RoisLang.asm;
using RoisLang.lower;
using RoisLang.mid_ir;
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

string test5 =
@"def increment(x: int) -> int:
    return increment(x + 1)

def sumall(a: int, b: int, c: int, d: int) -> int:
    return a + b + c + d

def wrongfib(n: int) -> int:
    return wrongfib(n - 1) + wrongfib(n - 2)
";

string test6 =
@"def id(x: int) -> int:
    return x

def tautology(x: int, y: int) -> int:
    return x * y * 2
";

string test7 =
@"
def fib(n: int) -> int:
    if n == 0:
        return 1
    if n == 1:
        return 1
    return fib(n - 1) + fib(n - 2)
";

//var tokens = Lexer.TokenizeString(test3);
var program = Parser.LexAndParse(test7);
new TypeChecker().TypeckProgram(program);
var lowerer = new AstLowerer();
var midFuncs = lowerer.LowerProgram(program);
midFuncs.ForEach(x => x.Dump());
Console.WriteLine();
/*foreach (var stmt in parseResult)
    lowerer.LowerStmt(stmt);*/
//new RegAlloc().RegAllocBlock(lowerer.GetBlock());
var output = File.Open("output.nasm", FileMode.Create);
AsmCompile.CompileAllFuncs(new StreamWriter(output, System.Text.Encoding.UTF8)/*Console.Out*/, midFuncs);
output.Close();


/*var a = MidValue.Reg(1, 0, TypeRef.VOID, Assertion.X);

MovesAlgorithm.CompileMoves(
    new MidValue[] { a, MidValue.ConstInt(20) },
    new GpReg[] { GpReg.Rax, GpReg.Rcx },
    new Dictionary<MidValue, GpReg>(
        new KeyValuePair<MidValue, GpReg>[] { new KeyValuePair<MidValue, GpReg>(a, GpReg.Rcx) })
    );*/

/*var a = MidValue.Reg(1, 0, TypeRef.VOID, Assertion.X);
var asmw = new AsmWriter(Console.Out, new Dictionary<MidValue, GpReg>(
        new KeyValuePair<MidValue, GpReg>[] { new KeyValuePair<MidValue, GpReg>(a, GpReg.Rcx) })
    );
asmw.WriteMoves(new MidValue[] { a }, new GpReg[] { GpReg.Rax }, true);*/