/*
Copyright (c) 2015 Joe Bogner joebogner@gmail.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


//building on mono: mcs microj.cs /define:CSSCRIPT /r:bin/CSScriptLibrary
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;

#if CSSCRIPT
using CSScriptLibrary;
#endif

namespace App {
    using MicroJ;
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args) {
            var argList = args.ToList();
            var jsIdx = argList.FindIndex(c => c.Contains("-js"));
            var debug = argList.FindIndex(c => c.Contains("-d")) > -1;
            var runRepl = argList.FindIndex(c => c.Contains("-i")) > -1 || args.Length == 0;
            if (debug) {
                Debugger.Launch();
            }

            if (args.Length > 0 && jsIdx > -1 && !runRepl) {
                int times = 1;

                var timeIdx = args.Take(jsIdx).ToList().FindIndex(c => c.Contains("-n"));

                if (timeIdx > -1) {
                    times = int.Parse(args[timeIdx + 1]);
                }

                long kbAtExecution = GC.GetTotalMemory(false) / 1024;
                var watch = new Stopwatch();
                var parser = new Parser();
                watch.Start();
                AType ret = null;
                var cmd = String.Join(" ", args.Skip(jsIdx + 1).ToArray());
                Console.WriteLine(cmd);
                try {
                    for (var i = 0; i < times; i++) {
                        ret = parser.parse(cmd);
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
                watch.Stop();
                if (ret != null) Console.WriteLine(ret.ToString());
                Console.WriteLine("Took: {0} ms", (watch.ElapsedMilliseconds) / (double)times);
                Console.WriteLine("Total: {0} ms", (watch.ElapsedMilliseconds));
                long kbAfter1 = GC.GetTotalMemory(false) / 1024;
                long kbAfter2 = GC.GetTotalMemory(true) / 1024;

                Console.WriteLine(kbAtExecution + " Started with this kb.");
                Console.WriteLine(kbAfter1 + " After the test.");
                Console.WriteLine(kbAfter1 - kbAtExecution + " Amt. Added.");
                Console.WriteLine(kbAfter2 + " Amt. After Collection");
                Console.WriteLine(kbAfter2 - kbAfter1 + " Amt. Collected by GC.");
            } else if (args.Length > 0 && args[0] == "-tp") {
                
            } else {
                var repl = new Parser();

                var files = new List<string>();
                if (File.Exists("stdlib.ijs")) { files.Add("stdlib.ijs"); }
                if (File.Exists("..\\stdlib.ijs")) { files.Add("..\\stdlib.ijs"); }
                if (args.Length > 0) { files.Add(args[0]); }

                bool testMode = argList.FindIndex(c => c.Contains("-t")) > -1;
                if (testMode) new Tests().TestAll();
                
                foreach(var file in files.Where(x=>!x.StartsWith("-"))) {
                    if (!File.Exists(file)) {
                        Console.WriteLine("file: " + file + " does not exist");
                        return;
                    }
                    testMode = argList.FindIndex(c => c.Contains("-t")) > -1;
                    bool quiet = argList.FindIndex(c => c.Contains("-q")) > -1;
                    if (file == "stdlib.ijs") { quiet = true; testMode = false;}
                    string[] lines = File.ReadAllLines(file);
                    for (var i = 0; i < lines.Length; i++) {
                        var line = lines[i];
                        try {
                            if (line.StartsWith("NB.") || line.Length == 0) continue;
                            if (line.StartsWith("exit")) break;
                            if (line.StartsWith("!B")) {
                                Debugger.Launch();
                                Debugger.Break();
                                line = line.Substring(2, line.Length - 2);
                            }
                            repl.ReadLine = () => {
                                i++;
                                return lines[i];
                            };
                            
                            var ret = repl.parse(line).ToString();
                            if (testMode && ret != "1" && !line.EndsWith("3 : 0")) {
                                var eqIdx = line.IndexOf("=");
                                var rerun = "";
                                if (eqIdx > -1) {
                                    rerun = repl.parse(line.Substring(0, eqIdx)).ToString();
                                }
                                eqIdx = line.IndexOf("-:");
                                if (eqIdx > -1) {
                                    rerun = repl.parse(line.Substring(0, eqIdx)).ToString();
                                }
                                Console.WriteLine("TEST FAILED - " + line + " returned " + ret + " output: " + rerun);
                            }
                            if (!quiet){
                                Console.WriteLine(ret);
                            }
                        } catch (Exception e) {
                            Console.WriteLine(line + "\n" + e);
                        }
                    }
                }
                if (runRepl) {
                    string prompt = "    ";
                    while (true) {
                        Console.Write(prompt);

                        repl.ReadLine = Console.ReadLine;
                        var line = Console.ReadLine();
                        if (line == null)
                            break;

                        // on Linux, pressing arrow keys will insert null characters to line
                        line = line.Replace("\0", "");
                        if (line == "exit")
                            break;

                        line = line.Trim();
                        if (line == "")
                            continue;

                        try {
                            var ret = repl.parse(line);                            
                            if (ret.GetCount() > 1000) {
                                var formatter = new Formatter(ret.Shape);
                                for (var i = 0; i < ret.GetCount() && i < 1000; i++) {
                                    formatter.Add(ret.GetString(i));
                                }
                                Console.WriteLine(formatter.ToString());
                            }
                            else {
                                Console.WriteLine(ret.ToString());
                            }
                            
                            
                        } catch (Exception e) {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
            }
        }
    }
    public class Tests {
        bool equals<T>(T[] a1, T[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }

        public void TestAll() {
            var j = new Parser();

            var tests = new Dictionary<string, Func<bool>>();

            Func<string, string[]> toWords = w => j.toWords(w);

            tests["returns itself"] = () => equals(toWords("abc"), new[] { "abc" });
            tests["parses spaces"] = () => equals(toWords("+ -"), new[] { "+", "-" });
            tests["parses strings"] = () => equals(toWords("1 'hello world' 2"), new[] { "1", "'hello world'", "2" });
            tests["parses strings with number"] = () => equals(toWords("1 'hello 2 world' 2"), new[] { "1", "'hello 2 world'", "2" });

            //todo failing
            //tests["parses strings with embedded quote"] = () => equals(toWords("'hello ''this'' world'"), new string[] { "'hello 'this' world'" });
            tests["parentheses"] = () => equals(toWords("(abc)"), new[] { "(", "abc", ")" });
            tests["parentheses2"] = () => equals(toWords("((abc))"), new[] { "(", "(", "abc", ")", ")" });
            tests["numbers"] = () => equals(toWords("1 2 3 4"), new[] { "1 2 3 4" });
            tests["floats"] = () => equals(toWords("1.5 2 3 4"), new[] { "1.5 2 3 4" });
            tests["op with numbers"] = () => equals(toWords("# 1 2 3 4"), new[] { "#", "1 2 3 4" });
            tests["op with numbers 2"] = () => equals(toWords("1 + 2"), new[] { "1", "+", "2" });
            tests["op with no spaces"] = () => equals(toWords("1+i. 10"), new[] { "1", "+", "i.", "10" });
            tests["op with no spaces 2-1"] = () => equals(toWords("2-1"), new[] { "2", "-", "1" });
            tests["op with no spaces i.5"] = () => equals(toWords("i.5"), new[] { "i.", "5" });
            tests["adverb +/"] = () => equals(toWords("+/ 1 2 3"), new[] { "+", "/", "1 2 3" });
            tests["no spaces 1+2"] = () => equals(toWords("1+2"), new[] { "1", "+", "2" });
            tests["copula abc =: '123'"] = () => equals(toWords("abc =: '123'"), new[] { "abc", "=:", "'123'" });
            tests["copula abc=:'123'"] = () => equals(toWords("abc=:'123'"), new[] { "abc", "=:", "'123'" });
            tests["|: i. 2 3"] = () => equals(toWords("|: i. 2 3"), new[] { "|:", "i.", "2 3" });

            tests["negative numbers _5 _6"] = () => equals(toWords("_5 _6"), new[] { "_5 _6" });
            tests["negative numbers _5 6 _3"] = () => equals(toWords("_5 6 _3"), new[] { "_5 6 _3" });

            tests["names with number"] = () => equals(toWords("a1b =: 1"), new[] { "a1b", "=:", "1" });
            tests["names with underscore"] = () => equals(toWords("a_b =: 1"), new[] { "a_b", "=:", "1" });
            tests["is with no parens"] = () => equals(toWords("i.3-:3"), new[] { "i.", "3", "-:", "3" });
            tests["foreign conjunction"] = () => equals(toWords("(15!:0) 'abc'"), new[] { "(", "15", "!:", ")", "0", "'abc'" });

            tests["boxing"] = () => equals(toWords("($ < i. 2 2)"), new[] { "(", "$", "<", "i.", "2 2", ")" });

            tests["verb assignment"] = () => {
                var parser = new Parser();
                parser.parse("plus=: +");
                var z = parser.parse("1 plus 2").ToString();
                return z == "3";
            };

            tests["double verb assignment"] = () => {
                var parser = new Parser();
                parser.parse("plus=: +");
                parser.parse("plusx=: plus");
                var z = parser.parse("1 plusx 2").ToString();
                return z == "3";
            };

            tests["rank conjunction /+\"1 i.3"] = () => equals(toWords("/+\"1 i.3"), new[] { "/", "+", "\"", "1", "i.", "3" });

            foreach (var key in tests.Keys) {
                if (!tests[key]()) {
                    //throw new ApplicationException(key);
                    Console.WriteLine("TEST " + key + " failed");
                }
            }
        }
    }
}

