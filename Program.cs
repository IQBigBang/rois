// See https://aka.ms/new-console-template for more information
using RoisLang.lower;
using RoisLang.parser;

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
@"def main(a, b):
    let c = a + b
    c = b + c
";

var tokens = Lexer.TokenizeString(test3);
var parseResult = Parser.LexAndParse(test3);
// TODO: type-check step
var lowerer = new AstLowerer();
lowerer.LowerFunc(parseResult);
/*foreach (var stmt in parseResult)
    lowerer.LowerStmt(stmt);*/

Console.WriteLine(tokens);