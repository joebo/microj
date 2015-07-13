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

            if (args.Length > 0 && jsIdx > -1) {
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
                new Tests().TestAll();
            } else {
                var repl = new Parser();

                var files = new List<string>();
                if (File.Exists("stdlib.ijs")) { files.Add("stdlib.ijs"); }
                if (args.Length > 0) { files.Add(args[0]); }

                foreach(var file in files.Where(x=>!x.StartsWith("-"))) {
                    if (!File.Exists(file)) {
                        Console.WriteLine("file: " + file + " does not exist");
                        return;
                    }
                    bool testMode = argList.FindIndex(c => c.Contains("-t")) > -1;
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
                            if (testMode && ret != "1") {
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
                            Console.WriteLine(ret.ToString());
                        } catch (Exception e) {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
            }
        }
    }
}

namespace MicroJ
{
    public abstract class AType
    {
        public long[] Shape;
        public int Rank { get { return Shape == null ? 0 : Shape.Length; } }
        public Type Type;

        public AType(Type t) {
            Type = t;
        }

        public AType Apply(Func<long, long> func) {
            var y = (A<long>) this;
            var z = new A<long>(y.Count, y.Shape);
            for(var i = 0; i < y.Count; i++) {
                z.Ravel[i] = func(y.Ravel[i]);
            }
            return z;
        }
        
        public AType Apply(Func<long, bool> func) {
            var y = (A<long>) this;
            var z = new A<bool>(y.Count, y.Shape);
            for(var i = 0; i < y.Count; i++) {
                z.Ravel[i] = func(y.Ravel[i]);
            }
            return z;
        }

        //fills a list of longs to a rectangular matrix based upon the shape and frame
        //frame is used to indicate shape of n-cells in zs
        public AType Fill(long[][] zs, long[] shape, long[] frame) {
            if (frame != null && frame.Length > 1) { throw new NotImplementedException("Rank > 1 frame not supported"); }
            var ncells = frame == null ? 1 : frame[0];
            var maxCt = zs.Max(x=>x.Length)/ncells;
            long[] newShape;
            if (ncells == 1) {
                newShape = shape.ToList().Concat(new long[] { maxCt }).ToArray();
            } else {
                newShape = shape.ToList().Concat(new long[] { ncells, maxCt }).ToArray();
            }
            var z = new A<long>(maxCt*ncells*zs.Length, newShape);
            var offset = 0;
            for(var i = 0; i < zs.Length; i++) {
                for(var q = 0; q < ncells; q++) {
                    var have = zs[i].Length/ncells;
                    for(var k = 0; k < maxCt; k++) {
                        z.Ravel[offset++] = k >= have ? 0 : zs[i][q*have+k];
                    }

                }
            }
            return z;

        }
        
        //takes a long and returns a list of longs
        public AType Apply(Func<long, long[]> func, long[] frame) {
            var y = (A<long>) this;
            var zs = new long[y.Count][];
            for(var i = 0; i < y.Count; i++) {
                zs[i] = func(y.Ravel[i]);
            }
            if (Rank == 0) {
                var z = new A<long>(zs[0].Length);
                if (frame != null) {
                    z.Shape = new long[] { zs[0].Length / frame[0], frame[0] };
                }
                z.Ravel = zs[0];
                return z;
            }
            else
                return Fill(zs, y.Shape, frame);
        }


        public long GetCount() {
            return AType.ShapeProduct(this.Shape);
        }
        public long GetLong(int n) {
            return ((A<long>)this).Ravel[n];
        }
        
        public static long ShapeProduct(long[] ri) {
            return ri.Aggregate(1L, (prod, next) => prod * next);
        }

        public A<double> ConvertDouble() {
            if (Type == typeof(double)) {
                return (A<double>)this;
            }
            else if (Type == typeof(long)) {
                var a = (A<long>)this;
                var z = new A<double>(a.Count, a.Shape);
                z.Ravel = a.Ravel.Select(x => (double)x).ToArray();
                return z;
            }
            throw new NotImplementedException();
        }


        public static AType MakeA(string word, Dictionary<string, AType> names) {
            int val;
            double vald;

            if (word.Length > 1 && word[0] == '_' && char.IsDigit(word[1])) {
                word = word.Replace("_", "-");
            }

            if (names != null && names.ContainsKey(word)) {
                return names[word];
            }
            else if (word.StartsWith("'")) {
                var str = word.Substring(1, word.Length - 2);
                var a = new A<JString>(1);
                a.Ravel[0] = new JString { str = str };
                a.Shape = new long[] { str.Length };
                return a;
            }
            if (word.Contains(" ") && !word.Contains(".")) {
                var longs = new List<long>();
                foreach (var tpart in word.Split(' ')) {
                    string part = tpart;

                    if (part.Length > 1 && part[0] == '_' && char.IsDigit(part[1])) {
                        part = part.Replace("_", "-");
                    }

                    longs.Add(int.Parse(part, CultureInfo.InvariantCulture));
                }
                var a = new A<long>(longs.Count);
                a.Ravel = longs.ToArray();
                return a;
            }
            else if (word.Contains(" ") && word.Contains(".")) {
                var doubles = new List<double>();
                foreach (var part in word.Split(' ')) {
                    doubles.Add(double.Parse(part, CultureInfo.InvariantCulture));
                }
                var a = new A<double>(doubles.Count);
                a.Ravel = doubles.ToArray();
                return a;
            }
            else if (int.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) {
                var a = new A<long>(0);
                a.Ravel[0] = val;
                return a;
            }
            else if (double.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out vald)) {
                var a = new A<double>(0);
                a.Ravel[0] = vald;
                return a;
            }
            else if (Verbs.Words.Contains(word)) {
                var a = new A<Verb>(1);
                a.Ravel[0] = new Verb { op = word };
                return a;
            }
            return new A<Undefined>(0);
        }
    }

    public struct Undefined { }

    public struct Verb {
        public string op;
        public string adverb;
        public string conj;
        public string rhs;
        public object childVerb;
        public override string ToString() {
            string str = "";
            if (op != null) str+=op;
            if (adverb != null) str+=" " + adverb;
            if (conj != null) str+=" " + conj;
            if (rhs != null) str+=" " + rhs;
            return str;
        }
    }

    //tbd should we use chars instead?
    public struct JString {
        public string str;
        public override string ToString() {
            //todo: determine if this is a good idea, needed for tests for now
            return str.Replace("\\n", "\n");
        }
    }
    public class A<T> : AType where T : struct {

        public T[] Ravel;
        public long Count { get { return Ravel.Length; } }

        public static Func<T, T, T> AddFunc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public A<T> ToAtom(long n) {
            var z = new A<T>(1);
            z.Ravel[0] = Ravel[n];
            return z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetAtom(AType a, long n) {
            return ((A<T>)a).Ravel[n];
        }

        //not used, but here for the future
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Add(T a, T b) {
            if (AddFunc == null) {
                var par1 = Expression.Parameter(typeof(T));
                var par2 = Expression.Parameter(typeof(T));

                var add = Expression.Add(par1, par2);

                AddFunc = Expression.Lambda<Func<T, T, T>>(add, par1, par2).Compile();
            }
            return AddFunc(a, b);
        }

        public A(long n) : base(typeof(T)) {
            //hold atoms
            Ravel = new T[n != 0 ? n : (n + 1)];
            if (n > 0) {
                Shape = new[] { n };
            }
        }

        public A(long n, long[] shape) : base(typeof(T)) {
            Ravel = new T[n];
            Shape = shape;
        }

        public string StringConverter(T val) {
            var str = "";
            if (typeof(T) == typeof(bool)) {
                str = Convert.ToBoolean(val) ? "1" : "0";
            }
            else if (typeof(T) == typeof(int)) {
                int v = (int)(object)val;
                if (v < 0) { return "_" + Math.Abs(v); }
                else { return v.ToString(); }
            }
            else if (typeof(T) == typeof(long)) {
                long v = (long)(object)val;
                if (v < 0) { return "_" + Math.Abs(v); }
                else { return v.ToString(); }
            }
            else if (typeof(T) == typeof(double)) {
                double v = (double)(object)val;
                if (v < 0) { return "_" + Math.Abs(v); }
                else if (double.IsInfinity(v)) { return "_"; }
                else if (v > 0) { return v.ToString(CultureInfo.InvariantCulture); }
                else if (double.IsNaN(v)) { return "0"; }
                else { return v.ToString(CultureInfo.InvariantCulture); }
            }
            else {
                str = val.ToString();
            }
            return str;
        }
        public override string ToString() {
            if (Rank == 0) {
                return StringConverter(Ravel[0]);
            }
            else {
                var z = new StringBuilder();
                long[] odometer = new long[Rank];
                for (var i = 0; i < Count; i++) {
                    z.Append(StringConverter(Ravel[i]));
                    if (typeof(T) != typeof(JString)) {
                        odometer[Rank - 1]++;
                    } else {
                        //hack since Ravel[0] is a string instance, not an array
                        odometer[Rank - 1] += ((JString)(object)Ravel[i]).str.Length;
                    }

                    if (odometer[Rank - 1] != Shape[Rank - 1]) {
                        z.Append(" ");
                    }

                    for (var k = Shape.Length - 1; k > 0; k--) {
                        if (odometer[k] == Shape[k]) {
                            odometer[k] = 0;
                            z.Append("\n");
                            odometer[k - 1]++;
                        }
                    }
                }
                var ret = z.ToString();
                ret = ret.Substring(0, ret.Length - (Shape.Length - 1));
                return ret;
            }
        }
    }

    public class Conjunctions {
        public static readonly string[] Words = new[] { "\"", "!:", "&", ":" };
        public Verbs Verbs;
        public Dictionary<string, AType> Names;
        
        public Conjunctions(Verbs verbs) {
            Verbs = verbs;

            var  path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            AppDomain currentDomain = AppDomain.CurrentDomain;

            var binPath = Path.Combine(path, "bin");
            currentDomain.AssemblyResolve +=  new ResolveEventHandler((sender,args) => {
                var assemblyPath = Path.Combine(binPath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath) == false) return null;
                Assembly dependency = Assembly.LoadFrom(assemblyPath);
                return dependency;
            });
        }

        public A<T> rank1ex<T>(AType method, A<T> y) where T : struct {
            var verb = ((A<Verb>)method).Ravel[0];
            var newRank = Convert.ToInt32(verb.rhs);

            //create a new verb without the conj component so we can safely pass it around
            var newVerb = new A<Verb>(1);
            if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;
                newVerb.Ravel[0] = cv;
            } else {
                newVerb.Ravel[0] = new Verb { op = verb.op, adverb = verb.adverb };
            }

            if (newRank == y.Rank) { return (A<T>)Verbs.Call1(newVerb, y); }

            var newShape = y.Shape.Take(y.Rank - newRank).ToArray();
            var newCt = AType.ShapeProduct(newShape);
            var vs = new A<T>[newCt];
            var subShape = y.Shape.Skip(y.Rank - newRank).ToArray();
            var subShapeCt = AType.ShapeProduct(subShape);
            var offset = 0;
            for (var i = 0; i < vs.Length; i++) {
                var newY = new A<T>(subShapeCt, subShape);
                for (var k = 0; k < newY.Count; k++) {
                    newY.Ravel[k] = y.Ravel[offset];
                    offset++;
                }
                vs[i] = (A<T>)Verbs.Call1(newVerb, newY);
            }
            var ct = vs.Length * vs[0].Count;

            if (vs[0].Shape != null) {
                newShape = newShape.Concat(vs[0].Shape).ToArray();
            }
            var v = new A<T>(ct, newShape);
            offset = 0;
            for (var i = 0; i < vs.Length; i++) {
                for (var k = 0; k < vs[0].Count; k++) {
                    v.Ravel[offset++] = vs[i].Ravel[k];
                }
            }
            return v;
        }

        //to use interop, download https://csscriptsource.codeplex.com/releases/view/614904
        //and put CSScriptLibrary.dll and Mono.CSharp.dll into the bin folder (relative to the exe)
        //future: add a boxed method that can take parameters
        //(3 2 $ 'abc')  (150!:0) 'return v.ToString();'
        //(3 2 $ 'abc')  (150!:0) 'return v.Ravel[0].str;'
        //(3 2 $ 'abc')  (150!:0) 'return v.Rank.ToString();'
        //(3 2 $ 1)  (150!:0) 'return v.ToString();'
        //'' (150!:0) 'System.Diagnostics.Debugger.Break();'
        //should the code be x or y?
        Dictionary<string, Func<AType, string>> dotnetMethodCache = null;
        public A<JString> calldotnet<T>(A<T> x, A<JString> y) where T : struct {

#if CSSCRIPT
            if (dotnetMethodCache == null ) { dotnetMethodCache = new Dictionary<string, Func<AType, string>>(); }
            Func<AType, string> func = null;
            if (!dotnetMethodCache.TryGetValue(y.Ravel[0].str, out func)) {
                var code = y.Ravel[0].str;
                var lines = code.Split('\n');
                var usings = String.Join("\n", lines.Where(t=>t.StartsWith("//css_using ")).Select(t=>"using " + t.Replace("//css_using ", "") +";").ToArray());
                var refs = lines.Where(t=>t.StartsWith("//css_ref ")).SelectMany(t=>t.Replace("//css_ref ", "").Split(',')).Select(t=>t.Trim()).ToArray();
                func  = CSScript.LoadDelegate<Func<AType, string>>(usings + "\n" + "string func (MicroJ.AType v) { " + code + " }", null, false, refs);
                dotnetMethodCache[y.Ravel[0].str] = func;
            }
            var ret = func(x);
            var v = new A<JString>(0);
            v.Ravel[0] = new JString { str = ret };
            return v;
#else
            var v = new A<JString>(0);
            v.Ravel[0] = new JString { str = "microj must be compiled with csscript support" };
            return v;
#endif
            
        }
        
        public AType Call1(AType method, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];

            //rank
            if (verb.conj == "\"") {
                //future: add special code for +/"n or use some type of integrated rank support
                if (y.GetType() == typeof(A<long>)) { return rank1ex(method, (A<long>)y); }
                //todo: evaluate performance of dynamic dispatch of rank -- probably ok
                else return Verbs.InvokeExpression("rank1ex", method, y, 1, this);
            }
            //bond
            else if (verb.conj == "&") {
                var x = AType.MakeA(verb.rhs, Names);
                var newVerb = new A<Verb>(0);
                newVerb.Ravel[0] = (Verb)verb.childVerb;
                //todo this order may not be right
                return Call2(newVerb,y,x);
                
            }
            else if (verb.conj == ":" && verb.op == "0") {
                if (verb.rhs == "0") {
                    var v = new A<JString>(0);
                    v.Ravel[0] = new JString { str = y.ToString() };
                    return v;
                }
            }
            throw new NotImplementedException(verb + " on y:" + y + " type: " + y.GetType());
        }

        public AType Call2(AType method, AType x, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            if (verb.conj == "!:" && verb.op == "150") {
                if (x.GetType() == typeof(A<JString>)) {
                    return (A<JString>)calldotnet((A<JString>) x, (A<JString>)y);
                }
                else if (x.GetType() == typeof(A<long>)) {
                    return (A<JString>)calldotnet((A<long>) x, (A<JString>)y);
                }
            }
            throw new NotImplementedException(verb + " on y:" + y + " type: " + y.GetType());
        }

    }
    public class Adverbs {
        public static readonly string[] Words = new[] { "/" };
        public Verbs Verbs;
        public Conjunctions Conjunctions;

        public Adverbs(Verbs verbs) {
            Verbs = verbs;
        }

        //todo move to utility
        public long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next) => prod * next);
        }

        //special code for +/ rank 1 (long)
        public A<long> reduceplus(A<long> y) {
            var v = new A<long>(0);
            long total = 0;
            for (var i = 0; i < y.Count; i++) {
                total += y.Ravel[i];
            }
            v.Ravel[0] = total;
            return v;
        }

        //special code for +/ rank 1 (double)
        public A<double> reduceplus(A<double> y) {
            var v = new A<double>(0);
            double total = 0;
            for (var i = 0; i < y.Count; i++) {
                total += (double)y.Ravel[i];
            }
            v.Ravel[0] = total;
            return v;
        }

        public A<T> reduce<T>(AType op, A<T> y) where T : struct {
            if (y.Rank == 1) {
                var v = new A<T>(0);
                v = (A<T>)Verbs.Call2(op, y.ToAtom(y.Count - 2), y.ToAtom(y.Count - 1));
                for (var i = y.Count - 3; i >= 0; i--) {
                    v = (A<T>)Verbs.Call2(op, y.ToAtom(i), v);
                }
                return v;
            } else {
                var newShape = y.Shape.Skip(1).ToArray();
                var ct = prod(newShape);
                var v = new A<T>(ct, newShape);
                for (var i = ct - 1; i >= 0; i--) {
                    for (var k = y.Shape[0] - 1; k >= 0; k--) {
                        var n = i + (k * ct);
                        if (k == y.Shape[0] - 1) {
                            var np = i + ((k - 1) * ct);
                            v.Ravel[i] = A<T>.GetAtom(Verbs.Call2(op, y.ToAtom(np), y.ToAtom(n)), 0);
                            k--;
                        } else {
                            v.Ravel[i] = A<T>.GetAtom(Verbs.Call2(op, y.ToAtom(n), v.ToAtom(i)), 0);
                        }
                    }
                }
                return v;
            }
            throw new NotImplementedException();
        }

        public A<T> table<T>(AType op, A<T> x, A<T> y) where T : struct {
            var ct = x.Count * y.Count;
            var shape = new[] { x.Count, y.Count };
            var v = new A<T>(ct, shape);
            long offset = 0;
            for (var xi = 0; xi < x.Count; xi++) {
                for (var yi = 0; yi < y.Count; yi++) {
                    var xt = new A<T>(0);
                    xt.Ravel[0] = x.Ravel[xi];

                    var yt = new A<T>(0);
                    yt.Ravel[0] = y.Ravel[yi];

                    v.Ravel[offset] = ((A<T>)Verbs.Call2(op, xt, yt)).Ravel[0];
                    offset++;
                }
            }
            return v;
        }
        public AType Call1(AType method, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            var adverb = verb.adverb;
            var op = verb.op;

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };

            //future: add check for integrated rank support (e.g. +/"1)
            if (verb.conj != null) {
                return Conjunctions.Call1(method, y);
            }

            //special code for +/
            if (adverb == "/" && op == "+" && y.Rank == 1 && y.GetType() == typeof(A<long>)) {
                return reduceplus((A<long>)y);
            }
            else if (adverb == "/" && op == "+" && y.Rank == 1 && y.GetType() == typeof(A<double>)) {
                return reduceplus((A<double>)y);
            }
            else if (adverb == "/" && op == "%" && y.GetType() == typeof(A<long>)) {
                //special code to convert longs to double for division
                var newY = y.ConvertDouble();
                return reduce<double>(newVerb, (A<double>)newY);
            }
            else if (adverb == "/") {
                if (y.GetType() == typeof(A<long>)) {
                    return reduce<long>(newVerb, (A<long>)y);
                }
                else if (y.GetType() == typeof(A<double>)) {
                    return reduce<double>(newVerb, (A<double>)y);
                }
            }
            throw new NotImplementedException(adverb + " on y:" + y + " type: " + y.GetType());
        }

        public AType Call2(AType verb, AType x, AType y) {
            var adverb = ((A<Verb>)verb).Ravel[0].adverb;
            var op = ((A<Verb>)verb).Ravel[0].op;

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };

            if (adverb == "/" && y.GetType() == typeof(A<long>) && x.GetType() == typeof(A<long>)) {
                return table(newVerb, (A<long>)x, (A<long>)y);
            }

            throw new NotImplementedException("ADV: " + adverb + " on x:" + x + " y:" + y + " type: " + y.GetType());
        }

    }

    public class VerbWithRank {
        public long Monadic;
        public long DyadicX;
        public long DyadicY;
        public Func<AType, AType, AType> DyadicFunc;
        public Func<AType, AType> MonadicFunc;
        public static long Infinite = long.MaxValue;
        public VerbWithRank(Func<AType, AType> monad, Func<AType, AType, AType> dyad, long monadic, long x, long y) {
            MonadicFunc = monad;
            DyadicFunc = dyad;
            Monadic = monadic;
            DyadicX = x;
            DyadicY = y;
        }
        
    }
    public class Verbs {

	public static readonly string[] Words = new[] { "+", "-", "*", "%", "i.", "$", "#", "=", "|:", "|.", "-:", "[", "p:", ","};
        public Adverbs Adverbs = null;
        public Conjunctions Conjunctions = null;

        //Func<A<long>, A<JString>, A<JString>> copyFunc;
        //Delegate copyFunc;

        Dictionary<Tuple<string, Type, Type>, Delegate> expressionDict;
        Dictionary<string, VerbWithRank> expressionMap;
        public Verbs() {
            expressionDict = new Dictionary<Tuple<string, Type, Type>, Delegate>();
            expressionMap = new Dictionary<string, VerbWithRank>();
            expressionMap["p:"] = new VerbWithRank(primesm, primes, 0, VerbWithRank.Infinite, VerbWithRank.Infinite);
        }

        public AType InvokeExpression(string op, AType x, AType y, int generics, object callee = null) {
            var key = new Tuple<string, Type, Type>(op, x.GetType(), y.GetType());
            Delegate d;
            if (!expressionDict.TryGetValue(key, out d)) {
                var calleeType = callee == null ? typeof(Verbs) : callee.GetType();

                MethodInfo meth;
                if (generics == 0) {
                    meth = calleeType.GetMethod(op);
                } else if (generics == 1) {
                    meth = calleeType.GetMethod(op).MakeGenericMethod(y.GetType().GetGenericArguments().First());
                } else {
                    meth = calleeType.GetMethod(op).MakeGenericMethod(x.GetType().GetGenericArguments().First(), y.GetType().GetGenericArguments().First());
                }
                var par1 = Expression.Parameter(x.GetType());
                var par2 = Expression.Parameter(y.GetType());
                var me = callee == null ? this : callee;
                var instance = Expression.Constant(me);
                var call = Expression.Call(instance, meth, par1, par2);

                d = Expression.Lambda(call, par1, par2).Compile();

                expressionDict[key] = d;
            }
            return (AType)d.DynamicInvoke(x, y);
        }

        public AType InvokeExpression(string op, AType y, object callee = null) {
            var key = new Tuple<string, Type, Type>(op, null, y.GetType());
            Delegate d;
            if (!expressionDict.TryGetValue(key, out d)) {
                var calleeType = callee == null ? typeof(Verbs) : callee.GetType();

                MethodInfo meth;
                meth = calleeType.GetMethod(op).MakeGenericMethod(y.GetType().GetGenericArguments().First());

                var par1 = Expression.Parameter(y.GetType());
                var me = callee == null ? this : callee;
                var instance = Expression.Constant(me);
                var call = Expression.Call(instance, meth, par1);

                d = Expression.Lambda(call, par1).Compile();

                expressionDict[key] = d;
            }

            return (AType)d.DynamicInvoke(y);
        }

        public A<long> iota<T>(A<T> y) where T : struct {
            var shape = y.Ravel.Cast<long>().ToArray();
            var ascending = shape.All(x => x >= 0);
            long ct = prod(shape);
            var k = Math.Abs(ct);
            var z = new A<long>(k);
            if (y.Rank > 0) { z.Shape = shape.Select(x => Math.Abs(x)).ToArray(); }
            z.Ravel = permutationIdx(z.Shape);
            if (!ascending) {
                for (var i = z.Rank - 1; i >= 0; i--) {
                    if (shape[i] < 0) {
                        var nr = z.Rank - i;
                        var conj = new A<Verb>(0);
                        conj.Ravel[0] = new Verb { op = "|.", conj = "\"", rhs = nr.ToString() };
                        z = Conjunctions.rank1ex(conj, z);
                    }
                }
            }
            return z;
        }

        public T Add<T, T2>(T a, T2 b) {
            return (T)((dynamic)a + ((T)(dynamic)b));
        }

        public A<T> math<T>(A<T> x, A<T> y, Func<T, T, T> op) where T : struct {
            var z = new A<T>(y.Ravel.Length, y.Shape);
            if (x.Rank == 0) {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
            }
            return z;
        }

        //dynamic dispatch of math operations -- slowest, around 7x slower
        public A<double> mathmixed(dynamic x, dynamic y, Func<dynamic, dynamic, dynamic> op) {
            var z = new A<double>(y.Ravel.Length, y.Shape);
            if (x.Rank == 0) {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
            }
            return z;
        }

        //convert long to double
        public A<double> mathmixed(A<long> x, A<double> y, Func<double, double, double> op) {
            var z = new A<double>(y.Ravel.Length, y.Shape);
            var newx = new A<double>(0);
            newx.Ravel[0] = ((A<long>)x).Ravel[0];

            if (x.Rank == 0) {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
            }
            return z;
        }

        public A<long> shape(AType y) {
            var v = new A<long>(y.Rank);
            if (y.Rank == 0) { v.Ravel[0] = 0; }
            else { v.Ravel = y.Shape; }
            return v;
        }

        public A<long> tally(AType y) {
            var v = new A<long>(0);
            if (y.Rank == 0) { v.Ravel[0] = 1; }
            else { v.Ravel[0] = y.Shape[0]; }
            return v;
        }

        
        public A<T> copy<T>(A<long> x, A<T> y) where T : struct {

            var copies = x.Ravel[0];
            var ct = copies * y.Count;
            var shape = y.Shape != null ? y.Shape : new long[] { 1 };
            shape[0] = shape[0] * copies;

            var v = new A<T>(ct, shape);
            long offset = 0;

            for (var n = 0; n < y.Count; n++) {
                for (var i = 0; i < copies; i++) {
                    v.Ravel[offset++] = y.Ravel[n];
                }
            }
            return v;
        }

        public A<T> transpose<T>(A<T> y) where T : struct {
            var shape = y.Shape.Reverse().ToArray();
            var v = new A<T>(y.Count, shape);
            var offsets = new long[y.Shape.Length];
            for (var i = 1; i <= y.Shape.Length - 1; i++) {
                offsets[(i - 1)] = prod(y.Shape.Skip(i).ToArray());
            }
            offsets[y.Shape.Length - 1] = 1;
            offsets = offsets.Reverse().ToArray();
            var idx = 0;
            long[] odometer = new long[shape.Length];
            for (var i = 0; i < y.Count; i++) {
                var offset = 0L;
                for (var k = y.Shape.Length - 1; k >= 0; k--) {
                    offset = offset + (offsets[k] * odometer[k]);
                }
                v.Ravel[idx] = y.Ravel[offset];
                idx++;

                odometer[shape.Length - 1]++;

                for (var k = y.Shape.Length - 1; k > 0; k--) {
                    if (odometer[k] == shape[k]) {
                        odometer[k] = 0;
                        odometer[k - 1]++;
                    }
                }
            }
            return v;
        }

        //permutation idx
        public long[] permutationIdx(long[] y, bool asc = true) {
            long tail = y.Length > 1 ? y.Skip(1).Aggregate((a, b) => a * b) : 1;
            var head = y[0];
            var v = new long[head * tail];
            if (asc) {
                for (var i = 0; i < head * tail; i++) {
                    v[i] = i;
                }
            }
            else {
                long offset = head * tail - 1;
                for (var i = 0; i < head; i++) {
                    var max = (i + 1) * tail;
                    for (var k = 0; k < tail; k++) {
                        v[offset--] = max - k - 1;
                    }
                };
            }
            return v;
        }

        public A<T> reverse<T>(A<T> y) where T : struct {
            var v = new A<T>(y.Count, y.Shape);
            var pidx = permutationIdx(y.Shape, false);
            for (var i = 0; i < y.Count; i++) {
                v.Ravel[i] = y.Ravel[pidx[i]];
            }
            return v;
        }

        public A<JString> reverse_str(A<JString> y) {
            var v = new A<JString>(y.Count, y.Shape);
            var ct = y.Count;

            for (var i = 0; i < y.Count; i++) {
                var str = new string(y.Ravel[ct - i - 1].str.ToCharArray().Reverse().ToArray());
                v.Ravel[i].str = String.Intern(str);
            }
            return v;
        }

        public long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next) => prod * next);
        }

        public A<T2> reshape<T2>(A<long> x, A<T2> y) where T2 : struct {

            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;
            var v = new A<T2>((int)ct, x.Ravel);
            for (var i = 0; i < ct; i++) {
                v.Ravel[i] = y.Ravel[offset];
                offset++;
                if (offset > ylen - 1) { offset = 0; }
            }
            return v;
        }

        public A<JString> reshape_str(A<long> x, A<JString> y) {
            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;

            char[] chars = new char[ct];
            ylen = y.Ravel[0].str.Length;

            for (var i = 0; i < ct; i++) {
                chars[i] = y.Ravel[0].str[(int)offset];
                offset++;
                if (offset > ylen - 1) { offset = 0; }
            }
            var size = x.Rank >= 1 ? x.Ravel[0] : 1;
            var len = x.Rank >= 1 ? x.Ravel[x.Rank] : x.Ravel[0];
            var v = new A<JString>(size, new[] { size, len });
            for (var i = 0; i < size; i++) {
                //intern string saves 4x memory in simple test and 20% slower
                v.Ravel[i].str = String.Intern(new String(chars, (int)(i * len), (int)len));
                //v.Ravel[i].str =  new String(chars, (int)(i*len), (int)len);
            }

            return v;
        }

        public A<bool> equals<T, T2>(A<T> x, A<T2> y) where T : struct where T2 : struct {
            //todo handle application errors without exceptions
            if (x.Count != y.Count) { throw new ArgumentException("Length Error"); }

            var z = new A<bool>(y.Count);
            for (var i = 0; i < y.Count; i++) {
                //todo: need a faster way to compare equality, this was failing on double to long comparisons
                //x.Ravel[i].Equals(y.Ravel[i]);
                z.Ravel[i] = x.StringConverter(x.Ravel[i]) == y.StringConverter(y.Ravel[i]);
            }
            return z;
        }

        public A<T> ravel<T>(A<T> y) where T : struct {
            var v = new A<T>(y.Count);
            for (var n = 0; n < y.Count; n++) {
                    v.Ravel[n] = y.Ravel[n];
            }
            return v;
        }

        public A<T> append<T>(A<T> x, A<T> y) where T : struct {
            if (x.Rank > 1 && AType.ShapeProduct(x.Shape) != AType.ShapeProduct(y.Shape)) throw new NotImplementedException("Rank > 1 non-equal shapes not implemented yet (need framing fill)");

            long[] newShape;
            if (y.Rank < 1) {
                newShape = new long[] { x.Count + y.Count };
            }
            else {
                var tail = y.Shape.Skip(1).ToArray();
                newShape = new long[] { x.Count + y.Count }.Concat(tail).ToArray();
            }
            var v = new A<T>(x.Count + y.Count, newShape);
            var offset=0;
            for (var n = 0; n < x.Count; n++) {
                v.Ravel[offset++] = x.Ravel[n];
            }
            for (var n = 0; n < y.Count; n++) {
                v.Ravel[offset++] = y.Ravel[n];
            }

            return v;
        }

        public AType primesm(AType w) {
            return primes(null, w);
        }
        public AType primes(AType  a, AType w) {
            var x = a != null ? (A<long>) a : new A<long>(0);
            var y = (A<long>) w;
            var xv = x.Ravel[0];
            Func<long, long> fl = null;
            Func<long, bool> fb = null;
            Func<long, long[]> fls = null;
            long[] frame = null;
            if (a == null) fl = Primes.GetNthPrime;
            else if (xv == -1)  fl = (l) => Primes.Pi((float)l);
            else if (xv == 0) fb = (l) => !Primes.IsPrime(l);
            else if (xv == 1) fb = Primes.IsPrime;
            else if (xv == 2) {
                frame = new long[] { 2 };
                fls = (l) => Primes.GetFactorsWithExponents(Primes.Factor(l).ToList()).ToArray();
            }

            else if (xv == 3) fls = (l) => Primes.Factor(l);

            if (fl != null) return y.Apply(fl);
            else if (fb != null) return y.Apply(fb);
            else if (fls != null) return y.Apply(fls, frame);
            
            else  throw new NotImplementedException();
        }

        
        public AType Call2(AType method, AType x, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            if (verb.adverb != null) {
                return Adverbs.Call2(method, x, y);
            }

            //future: add check for integrated rank support
            if (verb.conj != null) {
                return Conjunctions.Call2(method, x, y);
            }

            var op = verb.op;

            VerbWithRank verbWithRank = null;
            
            if (op == "+") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a + b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a + b);
                }

            }
            else if (op == "-") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a - b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a - b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a - b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a - b);
                }
            }
            else if (op == "*") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a * b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a * b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a * b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a * b);
                }
            }
            else if (op == "%") {
                var a2 = x.ConvertDouble();
                var b2 = y.ConvertDouble();
                return math(a2, b2, (a, b) => a / b);
            }
            else if (op == "$") {
                if (x.GetType() == typeof(A<long>)) {
                    if (y.GetType() == typeof(A<long>)) {
                        return reshape((A<long>)x, (A<long>)y);
                    }
                    else if (y.GetType() == typeof(A<double>)) {
                        return reshape((A<long>)x, (A<double>)y);
                    } else if (y.GetType() == typeof(A<JString>)) {
                        return reshape_str((A<long>)x, (A<JString>)y);
                    }
                }
            }
            else if (op == "=") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>))
                    return equals((A<long>)x, (A<long>)y);
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>))
                    return equals((A<double>)x, (A<double>)y);
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>))
                    return equals((A<long>)x, (A<double>)y);
                else if (x.GetType() == typeof(A<JString>) && y.GetType() == typeof(A<JString>))
                    return equals((A<JString>)x, (A<JString>)y);
                else return InvokeExpression("equals", x, y, 2);
            }
            else if (op == "#") {
                return InvokeExpression("copy", x, y, 1);
            }
            else if (op == ",") {
                return InvokeExpression("append", x, y, 1);
            }

            else if (op == "-:") {
                //temporary
                var z = new A<bool>(0);
                z.Ravel[0] = x.ToString() == y.ToString();
               return z;
            }
            else if (expressionMap.TryGetValue(op, out verbWithRank)) {
                if (verbWithRank.DyadicX == VerbWithRank.Infinite && verbWithRank.DyadicY == VerbWithRank.Infinite) {
                    return verbWithRank.DyadicFunc(x,y);
                }
            }

            throw new NotImplementedException(op + " on x:" + x + " y:" + y + " type: " + y.GetType());
        }

        //candidate for code generation
        public AType Call1(AType method, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            if (verb.adverb != null) {
                return Adverbs.Call1(method, y);
            }

            //future: add check for integrated rank support
            if (verb.conj != null) {
                return Conjunctions.Call1(method, y);
            }

            var op = verb.op;
            VerbWithRank verbWithRank = null;

            if (op == "i.") {
                if (y.GetType() == typeof(A<int>)) {
                    return iota((A<int>)y);
                }
                else if (y.GetType() == typeof(A<long>)) {
                    return iota((A<long>)y);
                }
            } else if (op == "$") {
                return shape(y);
            } else if (op == "#") {
                return tally(y);
            }
            else if (op == "|:") {
                if (y.GetType() == typeof(A<int>)) {
                    return transpose((A<int>)y);
                }
                else if (y.GetType() == typeof(A<long>)) {
                    return transpose((A<long>)y);
                }
            }else if (op == "|.") {
                if (y.GetType() == typeof(A<long>)) {
                    return reverse((A<long>)y);
                }
                else if (y.GetType() == typeof(A<JString>)) {
                    return reverse_str((A<JString>)y);
                }
                return InvokeExpression("reverse", y);
            }
            else if (op == ",") {
                return InvokeExpression("ravel", y);
            }
            else if (expressionMap.TryGetValue(op, out verbWithRank)) {
                return verbWithRank.MonadicFunc(y);
            }
            throw new NotImplementedException(op + " on y: " + y + " type: " + y.GetType());
        }
    }


    public class Parser {

        public Verbs Verbs;
        public Adverbs Adverbs;
        public Conjunctions Conjunctions;

        public Func<string> ReadLine = null;
        
        public Dictionary<string, AType> Names;

        char[] symbols = null;
        char[] symbolPrefixes = null;

        public Parser() {
            Names = new Dictionary<string, AType>();

            Verbs = new Verbs();
            Adverbs = new Adverbs(Verbs);
            Conjunctions = new Conjunctions(Verbs);
            
            Adverbs.Conjunctions = Conjunctions;

            Verbs.Adverbs = Adverbs;
            Verbs.Conjunctions = Conjunctions;


            Conjunctions.Names = Names;
            //symbols are the first letter of every verb or adverb, letter symbols and ':' cause problems currently
            symbols = Verbs.Words.Select(x => x[0]).Union(Adverbs.Words.Select(x => x[0])).Union(Conjunctions.Words.Select(x => x[0])).Where(x => !char.IsLetter(x) && x!=':').ToArray();
            symbolPrefixes = Verbs.Words.Where(x => x.Length > 1).Select(x => x[1]).Union(Adverbs.Words.Where(x => x.Length > 1).Select(x => x[1])).ToArray();
        }

        public string[] toWords(string w) {
            var z = new List<string>();
            var currentWord = new StringBuilder();

            //using trim is a hack
            var emit = new Action(() => { if (currentWord.Length > 0) { z.Add(currentWord.ToString().Trim()); } currentWord = new StringBuilder(); });
            char p = '\0';

            var commentIdx = w.IndexOf("NB.");
            if (commentIdx >= 1) {
                w = w.Substring(0, commentIdx);
            }

            if (w.EndsWith(": 0")) {
                w+= "'";
                while(true) {
                    var nextLine = ReadLine();
                    if (nextLine == ")") break;
                    w+=nextLine+"\n";
                }
                w = w.Substring(0, w.Length-1);
                w+="'";
            }

            
            Func<char, bool> isSymbol = c => symbols.Contains(c);
            Func<char, bool> isSymbolPrefix = c => c != '.' && symbolPrefixes.Contains(c);
            Func<char, bool> isDigit = c => char.IsDigit(c) || c == '_';
            bool inQuote = false;

            foreach (var c in w)
            {
                if (!inQuote && c == '\'') { emit(); currentWord.Append(c); inQuote = true; }
                else if (inQuote && c == '\'') { currentWord.Append(c); emit(); inQuote = !inQuote; }
                else if (inQuote) { currentWord.Append(c); }
                else {

                    if (c == '(' || c == ')') { emit(); currentWord.Append(c); emit(); }
                    else if (!isDigit(p) && c == ' ') { emit(); }
                    else if (p == ' ' && !isDigit(c)) { emit(); currentWord.Append(c); }
                    else if (isDigit(p) && c != ' ' && c != '.' && !isDigit(c) && !char.IsLetter(c)) { emit(); currentWord.Append(c); }
                    else if ((c == '.' && p == '=') || (c == ':' && p == '=')) { currentWord.Append(c); emit(); }
                    else if ((c == '.' && p == 'i')) { currentWord.Append(c); emit(); } //special case for letter symbols
                    else if (isSymbol(p) && char.IsLetter(c)) { emit(); currentWord.Append(c); }
                    else if (isSymbol(p) && isSymbol(c)) { emit(); currentWord.Append(c); emit(); }
                    else if ((isSymbol(p) || isSymbolPrefix(p)) && isDigit(c)) { emit(); currentWord.Append(c); } //1+2
                    else if (isSymbol(c) && !isSymbol(p)) { emit(); currentWord.Append(c); }
                    else currentWord.Append(c);
                }
                p = c;
            }
            emit();
            return z.ToArray();
        }

        public struct Token {
            public string word;
            public AType val;
            public override string ToString()  {
                string str = "";
                if (word != null) str += word;
                if (val != null) str+= " " + val;
                return str;
            }
        }

        public bool IsValidName(string word) {
            if (word == null) { return false; }
            return word.All(x => char.IsDigit(x) || char.IsLetter(x) || x == '_');
        }

        public AType parse(string cmd) {
            const string MARKER = "`";
            cmd = MARKER + " " + cmd;

            Func<Token, bool> isEdge = token => token.word == MARKER || token.word == "=:" || token.word == "(";
            Func<Token, bool> isVerb = token => (token.val != null && token.val.GetType() == typeof(A<Verb>)); //|| (token.word != null && verbs.ContainsKey(token.word));
            Func<Token, bool> isAdverb = token => token.word != null && Adverbs.Words.Contains(token.word);
            Func<Token, bool> isConj = token => token.word != null && Conjunctions.Words.Contains(token.word);
            Func<Token, bool> isNoun = token => (token.val != null && token.val.GetType() != typeof(A<Verb>));
            Func<Token, bool> isEdgeOrNotConj = token => isEdge(token) || isVerb(token) || isNoun(token) || token.word == "";
            Func<Token, bool> isName = token => IsValidName(token.word);

            var words = toWords(cmd);

            var stack = new Stack<Token>();
            var queue = new Queue<Token>();
            for (var k = words.Length - 1; k >= 0; k--) {
                queue.Enqueue(new Token { word = words[k] });
            }


            int i = 0;
            //just a safety check for now
            while (i < 100) {
                var sarr = stack.ToArray().ToList();
                var w1 = sarr.Count > 0 ? sarr[0] : new Token { word = "" };
                var w2 = sarr.Count > 1 ? sarr[1] : new Token { word = "" };
                var w3 = sarr.Count > 2 ? sarr[2] : new Token { word = "" };
                var w4 = sarr.Count > 3 ? sarr[3] : new Token { word = "" };
                //new Token[] { w1,w2,w3,w4 }.Dump();

                var step = -1;
                if (isEdge(w1) && isVerb(w2) && isNoun(w3) && true) { step = 0; }
                else if (isEdgeOrNotConj(w1) && isVerb(w2) && isVerb(w3) && isNoun(w4)) { step = 1; }
                else if (isEdgeOrNotConj(w1) && isNoun(w2) && isVerb(w3) && isNoun(w4)) { step = 2; }
                else if (isEdgeOrNotConj(w1) && (isNoun(w2) || isVerb(w2)) && isAdverb(w3) && true) { step = 3; } //adverb
                else if (isEdgeOrNotConj(w1) && (isNoun(w2) || isVerb(w2)) && isConj(w3) && (isNoun(w2) || isVerb(w2))) { step = 4; }
                else if ((isNoun(w1) || isName(w1)) && (w2.word == "=:" || w2.word == "=.") && true && true) { step = 7; }
                else if (w1.word == "(" && (isNoun(w2) || isVerb(w2)) && w3.word == ")" && true) { step = 8; }

                //Console.WriteLine(step);
                if (step >= 0) {
                    if (step == 0) { //monad
                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var y = stack.Pop();
                        var z = Verbs.Call1(op.val, y.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 1) {   //monad                         
                        var p1 = stack.Pop();
                        var p2 = stack.Pop();
                        var op = stack.Pop();
                        var x = stack.Pop();
                        var z = Verbs.Call1(op.val, x.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p2);
                        stack.Push(p1);

                    }
                    else if (step == 2) { //dyad
                        var p1 = stack.Pop();
                        var x = stack.Pop();
                        var op = stack.Pop();
                        var y = stack.Pop();
                        var z = Verbs.Call2(op.val, x.val, y.val);
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 3) { //adverb
                        var p1 = stack.Pop();
                        var op = stack.Pop();
                        var adv = stack.Pop();
                        var z = new A<Verb>(0);
                        z.Ravel[0] = ((A<Verb>)op.val).Ravel[0];
                        z.Ravel[0].adverb = adv.word;
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 4) { //conjunction
                        var p1 = stack.Pop();
                        var lhs = stack.Pop();
                        var conj = stack.Pop();
                        var rhs = stack.Pop();

                        //todo handle conjunction returning noun

                        //special case 0 : 'abc'
                        if (conj.word == ":" && lhs.word == "0" && rhs.val is A<JString>) {
                            stack.Push(new Token { val = rhs.val });
                        }
                        else {
                            var z = new A<Verb>(0);
                            if (isVerb(lhs)) {
                                //z.Ravel[0] = ((A<Verb>)lhs.val).Ravel[0];
                                z.Ravel[0].childVerb = ((A<Verb>)lhs.val).Ravel[0];
                            } else {
                                z.Ravel[0].op = lhs.word;
                            }
                            z.Ravel[0].conj = conj.word;
                            z.Ravel[0].rhs = rhs.word;
                            stack.Push(new Token { val = z });
                        }
                        stack.Push(p1);
                    }

                    else if (step == 7) { //copula
                        var name = stack.Pop();
                        var copula = stack.Pop();
                        var rhs = stack.Pop();
                        Names[name.word] = rhs.val;
                        stack.Push(rhs);
                    }
                    else if (step == 8) { //paren
                        var lpar = stack.Pop();
                        var x = stack.Pop();
                        var rpar = stack.Pop();
                        stack.Push(x);
                    }
                }
                else {
                    if (queue.Count() != 0) {
                        var newWord = queue.Dequeue();

                        //try to parse word before putting on stack
                        var val = AType.MakeA(newWord.word, Names);
                        var token = new Token();
                        if (val.GetType() == typeof(A<Undefined>)) {
                            token.word = newWord.word;
                        }
                        else {
                            token.val = val;
                            token.word = newWord.word;
                        }
                        stack.Push(token);
                    }
                    if (queue.Count() == 0 && (stack.Count() == 1 || stack.Count() == 2)) {
                        //Console.WriteLine("DONE");
                        break;
                    }
                }
                i++;
            }
            stack.Pop();
            var ret = stack.Pop();
            if (ret.val == null && ret.word != null) {
                var retv = new A<JString>(0);
                retv.Ravel[0] = new JString { str = ret.word };
                return retv;
            }
            else if (ret.val != null) {
                return ret.val;

            }
            throw new ApplicationException("no value found on stack - after " + i.ToString() + " iterations");
        }
    }

    public static class Primes {
        public static bool IsPrime (long n){
            if (n <= 1)
                return false; //should fail?

            if (n == 2)
                return true;

            if (n % 2 == 0)
                return false;

            for (int i = 3; i < Math.Sqrt (n) + 1; i += 2) {
                if (n % i == 0) {
                    return false;
                }
            }
            return true;
        }

        public static long GetNthPrime (long n){

            if (n < 0)
                return 0;// TODO throw an exception ??
            if (n == 0)
                return 2;
            long prime = 2;
            long i = 0;

            long count = 3; // start at 3
            while (i < n) {
                if (IsPrime (count) == true) {
                    prime = count;
                    i++;
                }
                count+=2;
            }
            return prime;
        }

        public static long Pi (float n){
            if (n <= 1)
                return 0;
            if (n < 3)
                return 1;
            long c = Pi ((float)Math.Pow (n, 1.0f / 3));

            long mu = Pi ((float)Math.Sqrt (n)) - c;
            return (long)(phi (n, c) + c * (mu + 1) + (mu * mu - mu) * 0.5f - 1 - SumPi (n, c, mu));
        }

        private static long SumPi (float m, long n, long mu){
            long i = 1;
            long total = 0;
            while (i <= mu) {
                total += Pi (m / GetNthPrime (n + i - 1));
                i++;
            }
            return total;
        }

        private static long phi (float m, long n) {
            if (m < 0 || n < 0)
                throw new System.Exception ("Arguments must be non-negative");
            if (n == 0) {
                return (long)m;
            } else {
                return phi (m, n - 1) - phi ((long)(m / (float)(GetNthPrime (n - 1))), n - 1);
            }
        }

        public  static long[] Factor (long n){
            List<long> lst = new List<long> ();
            if (n <= 1)
                return lst.ToArray(); //throw exception?

            long divisor = PollardRho (n, new Random ());
            lst.Add (divisor);
            lst.AddRange (Factor (n / divisor));
            return lst.ToArray();
        }

        //can be used for 2 p: y, possibly.
        private static Dictionary<long, int> GetFactorExponents (List<long> factorList){
            Dictionary<long, int> d = new Dictionary<long, int> ();
            foreach (long l in factorList) {
                if (d.ContainsKey (l))
                    d [l]++;
                else
                    d [l] = 1;
            }
            return d;
        }

        public static List<long> GetFactorsWithExponents(List<long> factorList){
            Dictionary<long,int> exponentDict = GetFactorExponents (factorList);
            List<long> keys = new List<long> ();
            List<long> vals = new List<long> ();
            foreach(KeyValuePair<long, int> kvp in exponentDict){
                keys.Add (kvp.Key);
                vals.Add ((long)kvp.Value);
            }
            keys.AddRange (vals);
            return keys;
        }


        //unused.
        private static long FactorizeSimple (long n, long previous){
            if (n % 2 == 0)
                return 2;
            if (n % 3 == 0)
                return 3;
            if (n % 5 == 0)
                return 5;

            long i;
            for (i = previous; i < Math.Sqrt (n) + 1; i += 2) {
                if (n % i == 0)
                    return i;
            }
            return n;
        }

        private static long PollardRho (long n, Random rand){

            if (n % 2 == 0)
                return 2;
            if (n % 3 == 0)
                return 3;
            if (n % 5 == 0)
                return 5;
            byte[] buffer = BitConverter.GetBytes (n);
            rand.NextBytes (buffer);
            long summand = BitConverter.ToInt64 (buffer, 0);
            rand.NextBytes (buffer);
            long a = BitConverter.ToInt64 (buffer, 0);
            long b = a;
            long divisor;
            //
            a = (n + a * a + summand) % n;
            b = (((n + b * b + summand) % n) * (n + (n + b * b + summand) % n) + summand) % n;
            divisor = GCD (a - b, n);

            while (divisor == 1) {
                a = (a * a + summand) % n;
                b = (((n + b * b + summand) % n) * (n + (n + b * b + summand) % n) + summand) % n;
                divisor = GCD (a - b, n);
            }
            return divisor;
        }

        private static long GCD (long a, long b){
            return b == 0 ? a : GCD (b, (b + a) % b);
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
            tests["foreign conjunction"] = () => equals(toWords("(15!:0) 'abc'"), new[] {"(",  "15", "!:", ")", "0", "'abc'" });

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
