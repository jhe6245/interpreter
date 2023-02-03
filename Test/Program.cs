﻿using Test;
using Test.Interpreter;
using Test.Lexer;
using Test.Parser;

/* todo:
closures ?
*/

const string stdlib = @"
let range := (offset, count) -> {
    let result := repeat(count)
    let i := 0
    loop(i < count) {
        set(result, i, i + offset)
        i := i + 1
    }
    result
}

let map := (list, f) -> {
    let result := repeat(len(list))
    let i := 0
    for(let item: list) {
        set(result, i, f(item))
        i := i + 1
    }
    result
}
";

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
",
    Lists = @"
let list := [1,2,3]
println(map(list, x -> -x))
println(range(10, 5))
"
};

var program = programs.Lists;


var lexer = new Lexer();

var stdLibTokens = lexer.Lex(stdlib).ToList();
var programTokens = lexer.Lex(program).ToList();

foreach (var (t, i) in programTokens.Select((t, i) => (t, i)))
    Console.WriteLine($"{i,10}: {t}");

Console.WriteLine("---");


var parser = new ParserBuilder().Build();

switch (parser.Accept(stdLibTokens))
{
    case IOk<IParsed<Test.Parser.Program>> o:
        var x = parser.Accept(programTokens);
        switch (x)
        {
            case IOk<IParsed<Test.Parser.Program>> ok:
                foreach (var s in ok.Result.Result.Statements)
                    Console.WriteLine(s.Pretty());
                Console.WriteLine("---");
                var i = new Interpreter();
                i.Execute(o.Result.Result);
                i.Execute(ok.Result.Result);
                break;
            case IErr<IParsed<Test.Parser.Program>> err:
                Console.WriteLine(err.Error);
                Console.WriteLine(programTokens.FindIndex(t => ReferenceEquals(t, err.Error)));
                Console.WriteLine(err.Trace);
                break;
        }
        break;

    case IErr<IParsed<Test.Parser.Program>> e:
        foreach (var (t, i) in stdLibTokens.Select((t, i) => (t, i)))
        {
            Console.WriteLine($"{i,10}: {t}{(ReferenceEquals(t, e.Error) ? new string('<', 50) : "")}");
        }
        Console.WriteLine(e.Trace);
        break;
}
