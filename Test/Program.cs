using Test;
using Test.Lexer;
using Test.Parser;

var p = @"
if (true) {
    print(true)
}
let t := []
print('hello world')
let f := x -> x
let x := 23
x := 13
x := f(x)
print(x)
let z := 'abc'
x := []
print(x)
x := [1, 2]
print(x)
let plus3 := map(x, f)
print(plus3)
print(x -> y)

if (false) {
    print(false)
}
";
p = @"let math := 2 - 2
print(math)
math := 1 + 2 * (8 + 2)
print(math)
for(let i: [1,2,3])
    print(i)
";

var ts = new Lexer().Lex(p).ToList();

foreach (var (t, i) in ts.Select((t, i) => (t, i)))
    Console.WriteLine($"{i,10}: {t}");

Console.WriteLine("---");


var parser = new ParserBuilder().Build();

switch (parser.Accept(ts))
{
    case IOk<IParsed<Test.Parser.Program>> ok:
        foreach (var x in ok.Result.Result.Statements)
            Console.WriteLine(x.Pretty());
        Console.WriteLine("---");
        new Interpreter().Execute(ok.Result.Result);
        break;
    case IErr<IParsed<Test.Parser.Program>> err:
        Console.WriteLine(err.Error);
        Console.WriteLine(ts.FindIndex(t => ReferenceEquals(t, err.Error)));
        Console.WriteLine(err.Trace);
        break;
}


