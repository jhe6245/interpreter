using Test;
using Test.Lexer;
using Test.Parser;

/* todo:
list accessors
closures ?
*/ 

var programs = new
{
    HelloWorld = @"print('hello world')",
    Factorial = @"
let fac := n -> {
    if(n <= 2)
       return n
    n * fac(n - 1)
}
print(fac(5))
",
    Operators = @"
println(true = true)
println(3 < 5)
println(3 <= 5)
println(3 > 5)
println(3 >= 5)
",
    Test = @"
let f := x -> {
    10
}
print(f())
"
};

var ts = new Lexer().Lex(programs.Factorial).ToList();

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


