// See https://aka.ms/new-console-template for more information
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
@"def main(a: int, b: int):
    let c = (a + b) + (b + a)
";

var tokens = Lexer.TokenizeString(test3);
var parseResult = Parser.LexAndParse(test3);
new TypeChecker().TypeckFunc(parseResult);
var lowerer = new AstLowerer();
lowerer.LowerFunc(parseResult);
/*foreach (var stmt in parseResult)
    lowerer.LowerStmt(stmt);*/

Console.WriteLine(tokens);