#WIP of a Tiny [J](http://www.jsoftware.com/) implementation in C#.
Motivation: I needed J's speed and expressiveness in C# for a webapp
Credits: JSoftware, Inc. for releasing the source code and for making excellent documentation available. Heavily influenced by [An Implementation of J](http://www.jsoftware.com/books/pdf/aioj.pdf)

Original version can be found in my [lang-lab](https://github.com/joebo/lang-lab/blob/master/csharp/jsharp/jsharp.cs). The initial version did not use generics and then was rewritten to its current form. Generics were chosen when I found I was adding type specific code to every verb.


## Status: Proof of concept

### Building / Running (tested on windows/linux)
1. run build.bat (windows) or mcs microj.cs (mono)
2. run microj.exe to get a repl, hit ctrl+c to exit or type exit

### Command Line Options
- filename 
- i to run the repl (with other arguments specified)
- -tp to run parser tests (see tests at bottom of file)
- -t to run the supplied file in test mode
- -q to suppress output on file run
- -js "1+1" to run a string
- -n 10 -js "+/ i. 5" to run an operations N times

### Primitive status

Starting with list from
[http://www.jsoftware.com/jwiki/DevonMcCormick/MinimalBeginningJ](http://www.jsoftware.com/jwiki/DevonMcCormick/MinimalBeginningJ)

| Primitive | Status                                          | Monad  | Dyad            |
|-----------|-------------------------------------------------|--------|-----------------|
| =.      | Not Implemented (HOLD) / waiting on local scope |        |                 |
| =:      | Implemented                                     |        |                 |
| _       | Implemented                                     |        |                 |
| NB.       | Implemented                                     |        |                 |
| $         | Implemented                                     | shape  | reshape         |
| #         | Implemented                                     | tally  | copy            |
| ,         | Partially Implemented (missing non-equal shapes)| ravel  | append          |
| ;         | Not Implemented                                 |        |                 |
| {.        | Not Implemented                                 |        |                 |
| }.        | Not Implemented                                 |        |                 |
| /         | Implemented                                     | insert | table           |
| i.        | Partially Implemented                                 | iota   |                 |
| +         | Partially Implemented                           |        | plus (math)     |
| *         | Partially Implemented                           |        | times (math)    |
| -         | Partially Implemented                           |        | subtract (math) |
| %         | Partially Implemented                           |        | divide (math)   |
| ^         | Not Implemented                                 |        |                 |
| ^.        | Not Implemented                                 |        |                 |
| <.        | Not Implemented                                 |        |                 |
| >.        | Not Implemented                                 |        |                 |

Data Types:
- integer (long) -- (consider int32 support)
- float (double)
- string
- bool (partial)
- boxed

## Todo:
1. More J primitives

## Vision:
Not intended to be a replacement for J. It may be useful to hack on to learn J better or embed in C#
