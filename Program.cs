// See https://aka.ms/new-console-template for more information
using RoisLang.asm;
using RoisLang.lower;
using RoisLang.mid_ir;
using RoisLang.opt;
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

def fib2(n: int) -> int:
    if n <= 1:
        return 1
    else:
        return fib(n - 1) + fib(n - 2)

def cftest(n: int) -> int:
    if 1 == 2:
        return n
    return 3*2*1

def branchtest(cond: bool, n: int) -> int:
    let x = 0
    if cond:
        x = 1
    else:
        x = 2
    return n * x
";

string test8 =
@"
class Point:
    val x: int
    val y: int

class IntList:
    val head: int
    val tail: IntList

def swap(p: Point):
    let temp = p.x
    p.x = p.y
    p.y = temp

def second(list: IntList) -> int:
    return list.tail.head

def nested(x: int) -> int:
    if x == 0:
        return 1
    else if x == 1:
        return 2
    else if x == 2:
        return 4
    else if x == 3:
        return 8
    else:
        return 100
";

string test9 =
@"
extern def magic_func(x: int, y: int) -> int
def test(x: int) -> int:
    return magic_func(magic_func(x, x), magic_func(x, x))
";

string test10 =
@"
extern def print_int(x: int)
class Point:
    val x: int
    val y: int

def main() -> int:
    let p = new Point(x: 2, y: 3)
    print_int(p.x)
    print_int(p.y)
    return 0
";

//var tokens = Lexer.TokenizeString(test3);
var program = Parser.LexAndParse(test10);
new TypeChecker().TypeckProgram(program);
var lowerer = new AstLowerer();
var midFuncs = lowerer.LowerProgram(program);
// opt passes
midFuncs.ForEach(x => ((IPass)new ConstantFold()).RunOnFunction(x));
midFuncs.ForEach(x => new RemoveDeadCode().RunOnFunction(x));
midFuncs.ForEach(x => x.Dump());
Console.WriteLine();
/*foreach (var stmt in parseResult)
    lowerer.LowerStmt(stmt);*/
//new RegAlloc().RegAllocBlock(lowerer.GetBlock());
var output = File.Open("../../../out/output.nasm", FileMode.Create);
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