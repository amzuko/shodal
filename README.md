Shodal is a C# port of (most of) the modal rewriting language: https://wiki.xxiivv.com/site/modal.

Shodal is at best a toy, but an instructive experience to port.

It is missing: many of the special registers, most arithmetic functions, undefining Rules.

It adds: the ability to define words that match before other words (via the <#) special form. Rules defined with this rule get matched before others. This allowed shodal to support a memoized fibonacci sequence implementation.

The most interesting aspect of porting modal was that it is very much a text-rewriting language. This meant that I constantly struggled around 'boxing' and 'unboxing' lists. A core cause of this is as follows:

```
<> (foo bar) baz
(foo bar)
```

In this example, the defined rule matches 'foo bar', but not '(foo bar)' The solution is pretty simple, but still surprising: enclose the head of the rule in an extra set of parenthesis.

Anyway, vacation is over, fun experiment.

Run it with:
```
dotnet run --project ./shodal examples/fibonacci_caching.shodal 1000
```
