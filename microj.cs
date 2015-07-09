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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Linq.Expressions; 
using System.Runtime.CompilerServices;
using System.Reflection;

namespace App {
    using MicroJ;
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 2 && args[1] == "-d") {
                System.Diagnostics.Debugger.Launch();                
            }
            if (args.Length > 0 && args[0] != "-t" && args[0] != "-i")
            {
                int times = 1;
                if (args.Length > 1 && args[1] == "-n") {
                    times = Int32.Parse(args[2]);
                }
                long kbAtExecution = GC.GetTotalMemory(false) / 1024;
                var watch = new Stopwatch();
                var parser = new Parser();
                watch.Start();
                AType ret = null;
                for(var i = 0; i < times; i++) {
                    ret = parser.parse(args[0]);
                }
                watch.Stop();
                Console.WriteLine(ret.ToString());
                Console.WriteLine(String.Format("Took: {0} ms", (watch.ElapsedMilliseconds)/ (double)times));
                Console.WriteLine(String.Format("Total: {0} ms", (watch.ElapsedMilliseconds)));
                long kbAfter1 = GC.GetTotalMemory(false) / 1024;
                long kbAfter2 = GC.GetTotalMemory(true) / 1024;

                Console.WriteLine(kbAtExecution + " Started with this kb.");
                Console.WriteLine(kbAfter1 + " After the test.");
                Console.WriteLine(kbAfter1 - kbAtExecution + " Amt. Added.");
                Console.WriteLine(kbAfter2 + " Amt. After Collection");
                Console.WriteLine(kbAfter2 - kbAfter1 + " Amt. Collected by GC.");       
            } else if (args.Length > 0 && args[0] == "-t") {
                new Tests().TestAll();
            }
            else {
                string prompt = "    ";
                string line = "";
                var parser = new Parser();
                while(true) {
                    Console.Write(prompt);

                    line = Console.ReadLine();
                    if(line == null)
                        break;

                    // on Linux, pressing arrow keys will insert null characters to line
                    line = line.Replace("\0", "");
                    if(line == "exit")
                        break;

                    line = line.Trim();
                    if(line == "")
                        continue;

                    try {
                        var ret = parser.parse(line);
                        Console.WriteLine(ret.ToString());
                    } catch (Exception e) {
                        Console.WriteLine(e.ToString());
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
            this.Type = t;
        }

        public long ShapeProduct(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }

        public A<double> ConvertDouble() {
            if (Type == typeof(double)) {
                return (A<double>) this;
            }
            else if (Type == typeof(long)) {
                var a = (A<long>)this;
                var z = new A<double>(a.Count, a.Shape);
                z.Ravel = a.Ravel.Select(x=>(double)x).ToArray();
                return z;
            }
            throw new NotImplementedException();
        }


        public static AType MakeA(string word, Parser environment)  {
            int val;
            double vald;

            if (word.Length > 1 && word[0] == '_' && Char.IsDigit(word[1])) {
                word = word.Replace("_", "-");
            }
            
            if (environment.Names.ContainsKey(word)) {
                return environment.Names[word];
            }
            else if (word.StartsWith("'")) {
                var str = word.Substring(1, word.Length-2);
                var a = new A<JString>(1);
                a.Ravel[0] = new JString { str = str };
                a.Shape = new long[] { str.Length };
                return a;
            }
            if (word.Contains(" ") && !word.Contains(".")) {
                var longs = new List<long>();
                foreach (var tpart in word.Split(' ')) {
                    string part = tpart;

                    if (part.Length > 1 && part[0] == '_' && Char.IsDigit(part[1])) {
                        part = part.Replace("_", "-");
                    }

                    longs.Add(Int32.Parse(part));
                }
                var a = new A<long>(longs.Count);
                a.Ravel = longs.ToArray();
                return a;
            }
            else if (word.Contains(" ") && word.Contains(".")) {
                var doubles = new List<double>();
                foreach (var part in word.Split(' ')) {
                    doubles.Add(Double.Parse(part));
                }
                var a = new A<double>(doubles.Count);
                a.Ravel = doubles.ToArray();
                return a;
            }
            else if (Int32.TryParse(word, out val)) {
                A<long> a = new A<long>(0);
                a.Ravel[0] = val;
                return a;
            }
            else if (Double.TryParse(word, out vald)) {
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
    }

    //tbd should we use chars instead?
    public struct JString {
        public string str;
        public override string ToString() {
            return str;
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
            return AddFunc(a,b);
        }
        
        public A(long n) : base(typeof(T)) {
            //hold atoms
            Ravel = new T[n != 0 ? n : (n+1)];
            if (n > 0) {
                Shape = new long[] { n };
            }
        }

        public A(long n, long[] shape ) : base(typeof(T)) {
            Ravel = new T[n];
            Shape = shape;
        }

        public string StringConverter(T val) {
            var str  = "";
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
                else if (v > 0) { return v.ToString(); }
                else if (double.IsNaN(v)) { return "0"; }
                else { return v.ToString(); }
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
                for(var i = 0; i < Count; i++) {
                    z.Append(StringConverter(Ravel[i]));
                    if (typeof(T) != typeof(JString)) {
                        odometer[Rank-1]++;
                    } else {
                        //hack since Ravel[0] is a string instance, not an array
                        odometer[Rank-1]+=((JString)(object)Ravel[i]).str.Length;
                    }

                    if (odometer[Rank-1] != Shape[Rank-1]) {
                        z.Append(" ");
                    }
                    
                    for(var k = Shape.Length-1;k>0;k--) {
                        if (odometer[k] == Shape[k]) {
                            odometer[k] = 0;
                            z.Append("\n");
                            odometer[k-1]++;
                        }
                    }
                }
                var ret = z.ToString();
                ret = ret.Substring(0, ret.Length - (Shape.Length-1));
                return ret;
            }
        }
    }

    public class Conjunctions {
        public static string[] Words = new string[] { "\"" };
        public Verbs Verbs;

        public Conjunctions(Verbs verbs) {
            Verbs = verbs;
        }

        public A<T> rank1ex<T>(AType method, A<T> y) where T : struct {
            var verb = ((A<Verb>) method).Ravel[0];
            var newRank = Convert.ToInt32(verb.rhs);

            //create a new verb without the conj component so we can safely pass it around
            var newVerb = new A<Verb>(1);
            newVerb.Ravel[0] = new Verb { op = verb.op, adverb = verb.adverb };

            if (newRank == y.Rank) { return (A<T>)Verbs.Call1(newVerb, y); }

            var newShape = y.Shape.Take(y.Rank-newRank).ToArray();
            var newCt = y.ShapeProduct(newShape);
            var vs = new A<T>[newCt];
            var subShape = y.Shape.Skip(y.Rank-newRank).ToArray();
            var subShapeCt = y.ShapeProduct(subShape);
            var offset = 0;
            for(var i = 0; i < vs.Length; i++) {
                var newY = new A<T>(subShapeCt, subShape);
                for(var k = 0; k < newY.Count; k++) {
                    newY.Ravel[k] = y.Ravel[offset];
                    offset++;
                }
                vs[i] = (A<T>)Verbs.Call1(newVerb, newY);
            }
            var ct = vs.Length * vs[0].Count;

            if (vs[0].Shape != null) {
                newShape = newShape.Union(vs[0].Shape).ToArray();
            }
            var v = new A<T>(ct, newShape );
            offset=0;
            for(var i = 0; i < vs.Length; i++) {
                for(var k = 0; k < vs[0].Count; k++) {
                    v.Ravel[offset++] = vs[i].Ravel[k];
                }
            }
            return v;
        }
        
        public AType Call1(AType method, AType y)  {
            var verb = ((A<Verb>) method).Ravel[0];

            if (verb.conj == "\"") {
                if (y.GetType() == typeof(A<long>)) { return rank1ex(method, (A<long>)y); }
                //todo: evaluate performance of dynamic dispatch of rank -- probably ok
                else return Verbs.InvokeExpression("rank1ex", method, y,1, this);
            }
            throw new NotImplementedException();
        }
    }
    public class Adverbs {
        public static string[] Words = new string[] { "/" };
        public Verbs Verbs;
        public Conjunctions Conjunctions;
        
        public Adverbs(Verbs verbs) {
            Verbs = verbs;
        }
        
        //todo move to utility
        public long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }

        //special code for +/ rank 1 (long)
        public A<long> reduceplus(A<long> y) {
            var v = new A<long>(0);
            long total = 0;
            for (var i = 0; i < y.Count; i++) {
                total+= (long)y.Ravel[i];
            }
            v.Ravel[0] = total;
            return v;
        }

        //special code for +/ rank 1 (double)
        public A<double> reduceplus(A<double> y) {
            var v = new A<double>(0);
            double total = 0;
            for (var i = 0; i < y.Count; i++) {
                total+= (double)y.Ravel[i];
            }
            v.Ravel[0] = total;
            return v;
        }

        public A<T> reduce<T>(AType op, A<T> y) where T : struct {
            if (y.Rank == 1) {
                var v = new A<T>(0);
                v = (A<T>)Verbs.Call2(op, y.ToAtom(y.Count-2), y.ToAtom(y.Count-1)); 
                for (var i = y.Count - 3; i >= 0; i--) {
                    v = (A<T>)Verbs.Call2(op, y.ToAtom(i), v); 
                }
                return v;
            } else {
                var newShape = y.Shape.Skip(1).ToArray();
                var ct = prod(newShape);
                var v = new A<T>(ct, newShape);
                for(var i = ct-1; i>=0; i--) {
                    for(var k = y.Shape[0]-1; k >= 0;k--) {
                        var n = i+(k*ct);
                        if (k == y.Shape[0]-1) {
                            var np = i+((k-1)*ct);
                            v.Ravel[i] = A<T>.GetAtom(Verbs.Call2(op, y.ToAtom(np), y.ToAtom(n)),0);
                            k--;
                        } else {
                            v.Ravel[i] = A<T>.GetAtom(Verbs.Call2(op, y.ToAtom(n), v.ToAtom(i)),0);
                        }
                    }
                }
                return v;
            }
            
            throw new NotImplementedException();
        }

        public A<T> table<T>(AType op, A<T> x, A<T> y) where T : struct {
            var ct = x.Count * y.Count;
            var shape = new long[] { x.Count, y.Count };
            var v = new A<T>(ct, shape);
            long offset = 0;
            for(var xi = 0; xi < x.Count; xi++) {
                for(var yi = 0; yi < y.Count; yi++) {
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
        public AType Call1(AType method, AType y)  {
            var verb = ((A<Verb>)method).Ravel[0];
            var adverb = verb.adverb;
            var op = verb.op;

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };

            //future: add check for integrated rank support
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
            throw new NotImplementedException();
        }

        public AType Call2(AType verb, AType x, AType y)  {
            var adverb = ((A<Verb>)verb).Ravel[0].adverb;
            var op = ((A<Verb>)verb).Ravel[0].op;

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };

            if (adverb == "/" && y.GetType() == typeof(A<long>) && x.GetType() == typeof(A<long>)) {
                return table(newVerb, (A<long>)x, (A<long>)y);
            }

            throw new NotImplementedException();
        }

    }
    
    public class Verbs {

        public static string[] Words = new string[] { "+", "-", "*", "%", "i.", "$", "#", "=", "|:" };
        public Adverbs Adverbs=null;
        public Conjunctions Conjunctions = null;
        
        //Func<A<long>, A<JString>, A<JString>> copyFunc;
        //Delegate copyFunc;

        Dictionary<Tuple<string, Type, Type>, Delegate> expressionDict;
        public Verbs() {
            expressionDict = new Dictionary<Tuple<string, Type, Type>, Delegate>();
        }

        public AType InvokeExpression(string op, AType x, AType y, int generics, object callee=null) {
            var key = new Tuple<string, Type, Type>(op, x.GetType(), y.GetType());
            Delegate d;
            if (!expressionDict.TryGetValue(key, out d)) {
                var calleeType = callee == null ? typeof(Verbs) : callee.GetType();

                MethodInfo meth;
                if (generics == 1) {
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

            return (AType) d.DynamicInvoke(x,y);
        }
        
        public A<long> iota<T>(A<T> y) where T : struct  {
            var shape = y.Ravel.Cast<long>().ToArray();
            var ascending = shape.All(x=>x>=0);
            long ct = prod(shape);
            var k = Math.Abs(ct);
            var z = new A<long>(k);
            if (y.Rank > 0) { z.Shape = shape.Select(x=>Math.Abs(x)).ToArray(); }
            //todo not implemented shape with different signs 3 _3 3
            if (ascending) {
                for(var i = 0; i < k; i++) {
                    z.Ravel[i] = i;
                }
            } else {
                for(var i = k-1; i >= 0; i--) {
                    z.Ravel[(k-i-1)] = i;
                }
            }
            return z;
        }

        T Add<T, T2>(T a, T2 b) {
            return (T) ((dynamic)a+((T)(dynamic)b));
        }

        //lambdas add about 3% overhead based upon tests of 100 times, for now worth it for code clarity
        public A<long> mathi(A<long> x, A<long> y, Func<long, long, long> op) { 
            var z = new A<long>(y.Ravel.Length, y.Shape);

            //split out for performance reasons/otherwise rank was evaluated in the loop
            if (x.Rank == 0) {
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
            }
            return z;
        }
        public A<double> mathd(A<double> x, A<double> y, Func<double, double, double> op) { 
            var z = new A<double>(y.Ravel.Length, y.Shape);
            if (x.Rank == 0) {
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
            }
            return z;
        }

        //dynamic dispatch of math operations -- slowest, around 7x slower
        public A<double> mathmixed(dynamic x, dynamic y, Func<dynamic, dynamic, dynamic> op) { 
            var z = new A<double>(y.Ravel.Length, y.Shape);
            if (x.Rank == 0) {
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for(var i = 0; i < y.Ravel.Length; i++) {
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
                for(var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
            }
            else {
                for(var i = 0; i < y.Ravel.Length; i++) {
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

        public A<T> copy<T>(A<long> x, A<T> y) where T: struct {
            
            var copies = x.Ravel[0];
            var ct = copies * y.Count;
            var shape = y.Shape != null ? y.Shape : new long[] { 1 };
            shape[0] = shape[0] * copies;
            
            var v = new A<T>(ct, shape);
            long offset = 0;

            for(var n = 0; n < y.Count; n++) {
                for(var i = 0; i < copies; i++) {
                    v.Ravel[offset++] = y.Ravel[n];
                }
            }
            return v;
        }

        public A<T> transpose<T> (A<T> y) where T : struct {
            var shape = y.Shape.Reverse().ToArray();
            var v = new A<T>(y.Count, shape);
            var offsets = new long[y.Shape.Length];
            for(var i = 1; i <= y.Shape.Length-1; i++) {
                offsets[(i-1)] = prod(y.Shape.Skip(i).ToArray());
            }
            offsets[y.Shape.Length-1] = 1;
            offsets = offsets.Reverse().ToArray();
            var idx = 0;
            long[] odometer = new long[shape.Length];
            for(var i = 0; i < y.Count; i++) {
                var offset = 0L;
                for(var k = y.Shape.Length-1;k>=0;k--) {
                    offset = offset + (offsets[k] * odometer[k]);
                }
                v.Ravel[idx] = y.Ravel[offset];
                idx++;

                odometer[shape.Length-1]++;

                for(var k = y.Shape.Length-1;k>0;k--) {
                    if (odometer[k] == shape[k]) {
                        odometer[k] = 0;
                        odometer[k-1]++;
                    }
                }
            }
            return v;
        }
        
        public long prod(long[] ri) {
            return ri.Aggregate(1L, (prod, next)=> prod*next);
        }

        public A<T2> reshape<T2>(A<long> x, A<T2> y) where T2 : struct {

            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;
            var v = new A<T2>((int)ct, x.Ravel);
            for(var i = 0; i < ct; i++ ) {
                v.Ravel[i] = y.Ravel[offset];
                offset++;
                if (offset > ylen-1) { offset = 0; }
            }
            return v;
        }

        public A<JString> reshape_str(A<long> x, A<JString> y) {
            var ct = prod(x.Ravel);
            long offset = 0;
            var ylen = y.Count;

            char[] chars = new char[ct];
            ylen = y.Ravel[0].str.Length;
            
            for(var i = 0; i < ct; i++ ) {
                chars[i] = y.Ravel[0].str[(int)offset];
                offset++;
                if (offset > ylen-1) { offset = 0; }
            }
            var size = x.Rank >= 1 ? x.Ravel[0] : 1;
            var len = x.Rank  >= 1 ?  x.Ravel[x.Rank] : x.Ravel[0];
            var v = new A<JString>(size, new long[] { size, len });
            for(var i = 0; i < size; i++) {
                //intern string saves 4x memory in simple test and 20% slower
                v.Ravel[i].str =  String.Intern(new String(chars, (int)(i*len), (int)len));
                //v.Ravel[i].str =  new String(chars, (int)(i*len), (int)len);
            }
            
            return v;
        }

        public A<bool> equals<T,T2>(A<T> x, A<T2> y) where T : struct where T2 : struct {
            //todo handle application errors without exceptions
            if (x.Count != y.Count) { throw new ArgumentException("Length Error"); }
            var z = new A<bool>(y.Count);
            for(var i = 0; i < y.Count; i++) {
                z.Ravel[i] = x.Ravel[i].Equals(y.Ravel[i]);
            }
            return z;
        }

        
        public AType Call2(AType method, AType x, AType y)  {
            var verb = ((A<Verb>) method).Ravel[0];
            if (verb.adverb != null) {
                return Adverbs.Call2(method, x, y);
            }
            var op = verb.op;


            if (op == "+") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return mathi((A<long>)x,(A<long>)y, (a,b)=>a+b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) { 
                    return mathd((A<double>)x,(A<double>)y, (a,b)=>a+b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x,(A<double>)y, (a,b)=>a+b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x,y, (a,b)=>a+b);
                }
                
            }
            else if (op == "-") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return mathi((A<long>)x,(A<long>)y, (a,b)=>a-b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return mathd((A<double>)x,(A<double>)y, (a,b)=>a-b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x,(A<double>)y, (a,b)=>a-b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x,y, (a,b)=>a-b);
                }
            }
            else if (op == "*") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return mathi((A<long>)x,(A<long>)y, (a,b)=>a*b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return mathd((A<double>)x,(A<double>)y, (a,b)=>a*b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x,(A<double>)y, (a,b)=>a*b);
                }                
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x,y, (a,b)=>a*b);
                }
            }
            else if (op == "%") {
                var a2 = x.ConvertDouble();
                var b2 = y.ConvertDouble();
                return mathd(a2,b2, (a,b)=>a/b);
            }
            else if (op == "$") {
                if (x.GetType() == typeof(A<long>)) {
                    if (y.GetType() == typeof(A<long>)) {
                        return reshape((A<long>)x,(A<long>)y);
                    }
                    else if (y.GetType() == typeof(A<double>)) {
                        return reshape((A<long>)x,(A<double>)y);
                    } else if (y.GetType() == typeof(A<JString>)) {
                        return reshape_str((A<long>)x,(A<JString>)y);
                    }
                }
            }
            else if (op == "=") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>))
                    return equals((A<long>)x,(A<long>)y);
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>))
                    return equals((A<double>)x,(A<double>)y);
                else if (x.GetType() == typeof(A<JString>) && y.GetType() == typeof(A<JString>))
                    return equals((A<JString>)x,(A<JString>)y);
                else return InvokeExpression("equals", x, y,2);
            }
            else if (op == "#") {
                return InvokeExpression("copy", x, y,1);
            }
            throw new NotImplementedException();
        }

        //candidate for code generation
        public AType Call1(AType method, AType y)  {
            var verb = ((A<Verb>) method).Ravel[0];
            if (verb.adverb != null) {
                return Adverbs.Call1(method, y);
            }

            //future: add check for integrated rank support
            if (verb.conj != null) {
                return Conjunctions.Call1(method, y);
            }
            
            var op = verb.op;
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
            }
            throw new ArgumentException();
        }
    }


    public class Parser {

        public Verbs Verbs;
        public Adverbs Adverbs;
        public Conjunctions Conjunctions;
        
        public Dictionary<string, AType> Names;

        char[] symbols = null;
        char[] symbolPrefixes = null;
        
        public Parser() {
            Verbs = new Verbs();
            Adverbs = new Adverbs(Verbs);
            Conjunctions = new Conjunctions(Verbs);

            Adverbs.Conjunctions = Conjunctions;
            
            Verbs.Adverbs = Adverbs;
            Verbs.Conjunctions = Conjunctions;

            Names = new Dictionary<string, AType>();

            //symbols are the first letter of every verb or adverb, letter symbols cause problems currently
            symbols = Verbs.Words.Select(x=>x[0]).Union(Adverbs.Words.Select(x=>x[0])).Union(Conjunctions.Words.Select(x=>x[0])).Where(x=>!Char.IsLetter(x)).ToArray();
            symbolPrefixes = Verbs.Words.Where(x=>x.Length>1).Select(x=>x[1]).Union(Adverbs.Words.Where(x=>x.Length>1).Select(x=>x[1])).ToArray();
        }
        
        public string[] toWords(string w) {
            var z = new List<string>();
            var currentWord = new StringBuilder();

            //using trim is a hack
            var emit = new Action(() => { if (currentWord.Length > 0) { z.Add(currentWord.ToString().Trim()); } currentWord = new StringBuilder(); });
            char p = '\0';

            Func<char, bool> isSymbol = (c) => symbols.Contains(c); 
            Func<char, bool> isSymbolPrefix = (c) => symbolPrefixes.Contains(c);
            Func<char, bool> isDigit = (c) => Char.IsDigit(c) || c == '_';
            bool inQuote = false;

            foreach (var c in w)
            {
                if (!inQuote && c == '\'') {  emit(); currentWord.Append(c); inQuote=true; }
                else if (inQuote && c == '\'') { currentWord.Append(c); emit();  inQuote = !inQuote; }
                else if (inQuote) { currentWord.Append(c); }
                else {
                    
                    if (c == '(' || c == ')') { emit(); currentWord.Append(c); emit(); }
                    else if (!isDigit(p) && c == ' ') { emit(); }
                    else if (p == ' ' && !isDigit(c)) { emit(); currentWord.Append(c); }
                    else if (isDigit(p) && c != ' ' && c!= '.' && !isDigit(c) && !Char.IsLetter(c)) { emit(); currentWord.Append(c); }
                    else if ((c == '.' && p == '=') || (c==':' && p== '=')) { currentWord.Append(c); emit(); }
                    else if ((c == '.' && p == 'i')) { currentWord.Append(c); emit(); } //special case for letter symbols
                    else if (isSymbol(p) && Char.IsLetter(c)) { emit(); currentWord.Append(c); }
                    else if (isSymbol(p) && isSymbol(c)) { emit(); currentWord.Append(c); emit(); }
                    else if (isSymbol(p) && isDigit(c)) { emit(); currentWord.Append(c); } //1+2
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
        }

        public bool IsValidName(string word) {
            if (word == null) { return false; }
            return word.All(x=>Char.IsDigit(x) || Char.IsLetter(x) || x == '_');
        }
        
        public AType parse(string cmd) {
            var MARKER = "`";
            cmd = MARKER + " " + cmd;

            Func<Token, bool> isEdge = (token) => token.word == MARKER || token.word == "=:" || token.word == "(";
            Func<Token, bool> isVerb = (token) => (token.val != null && token.val.GetType() == typeof(A<Verb>)); //|| (token.word != null && verbs.ContainsKey(token.word));
            Func<Token, bool> isAdverb = (token) => token.word != null && Adverbs.Words.Contains(token.word);
            Func<Token, bool> isConj = (token) => token.word != null && Conjunctions.Words.Contains(token.word);
            Func<Token, bool> isNoun = (token) => (token.val != null && token.val.GetType() != typeof(A<Verb>));
            Func<Token, bool> isEdgeOrNotConj = (token) => isEdge(token) || isVerb(token) || isNoun(token) || token.word == "";
            Func<Token, bool> isName = (token) => IsValidName(token.word);
            
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
                else if (isEdgeOrNotConj(w1) && (isNoun(w2) || isVerb(w2)) && isConj(w3) &&  (isNoun(w2) || isVerb(w2))) { step = 4; }
                else if ((isNoun(w1) || isName(w1)) && (w2.word == "=:" || w2.word == "=.") && true && true) { step = 7; }
                else if (w1.word == "(" && isNoun(w2) && w3.word == ")" && true) { step = 8; }

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
                        var z = new A<Verb>(0);
                        //todo handle conjunction returning noun
                        z.Ravel[0] = ((A<Verb>)lhs.val).Ravel[0];
                        z.Ravel[0].conj = conj.word;
                        z.Ravel[0].rhs = rhs.word;
                        stack.Push(new Token { val = z });
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
                        var val = AType.MakeA(newWord.word, this);
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
    public class Tests {
        bool equals<T>(T[] a1, params T[] a2) {
            return a1.OrderBy(a => a).SequenceEqual(a2.OrderBy(a => a));
        }

        public void TestAll() {
            var j = new Parser();

            Func<string, AType> parse = (cmd) => j.parse(cmd);


            var tests = new Dictionary<string, Func<bool>>();

            Func<string, string[]> toWords = (w) => j.toWords(w);
                
            tests["returns itself"] = () => equals(toWords("abc"), "abc");
            tests["parses spaces"] = () => equals(toWords("+ -"), new string[] { "+", "-" });
            tests["parses strings"] = () => equals(toWords("1 'hello world' 2"), new string[] { "1", "'hello world'", "2" });
            tests["parses strings with number"] = () => equals(toWords("1 'hello 2 world' 2"), new string[] { "1", "'hello 2 world'", "2" });

            //todo failing
            //tests["parses strings with embedded quote"] = () => equals(toWords("'hello ''this'' world'"), new string[] { "'hello 'this' world'" });
            tests["parentheses"] = () => equals(toWords("(abc)"), new string[] { "(", "abc", ")" });
            tests["parentheses2"] = () => equals(toWords("((abc))"), new string[] { "(", "(", "abc", ")", ")" });
            tests["numbers"] = () => equals(toWords("1 2 3 4"), new string[] { "1 2 3 4" });
            tests["floats"] = () => equals(toWords("1.5 2 3 4"), new string[] { "1.5 2 3 4" });
            tests["op with numbers"] = () => equals(toWords("# 1 2 3 4"), new string[] { "#", "1 2 3 4" });
            tests["op with numbers 2"] = () => equals(toWords("1 + 2"), new string[] { "1", "+", "2" });
            tests["op with no spaces"] = () => equals(toWords("1+i. 10"), new string[] { "1", "+", "i.", "10" });
            tests["op with no spaces 2-1"] = () => equals(toWords("2-1"), new string[] { "2", "-", "1" });
            tests["op with no spaces i.5"] = () => equals(toWords("i.5"), new string[] { "i.", "5" });
            tests["adverb +/"] = () => equals(toWords("+/ 1 2 3"), new string[] { "+", "/", "1 2 3" });
            tests["no spaces 1+2"] = () => equals(toWords("1+2"), new string[] { "1", "+", "2" });
            tests["copula abc =: '123'"] = () => equals(toWords("abc =: '123'"), new string[] { "abc", "=:", "'123'" });
            tests["copula abc=:'123'"] = () => equals(toWords("abc=:'123'"), new string[] { "abc", "=:", "'123'" });
            tests["|: i. 2 3"] = () => equals(toWords("|: i. 2 3"), new string[] { "|:", "i.", "2 3" });

            tests["negative numbers _5 _6"] = () => equals(toWords("_5 _6"), new string[] { "_5 _6" });
            tests["negative numbers _5 6 _3"] = () => equals(toWords("_5 6 _3"), new string[] { "_5 6 _3" });

            tests["names with number"] = () => equals(toWords("a1b =: 1"), new string[] { "a1b", "=:", "1" });
            tests["names with underscore"] = () => equals(toWords("a_b =: 1"), new string[] { "a_b", "=:", "1" });

            tests["verb assignment"] =() => {
                var parser = new Parser();
                parser.parse("plus=: +");
                var z =  parser.parse("1 plus 2").ToString();
                return z == "3";
            };

            tests["double verb assignment"] =() => {
                var parser = new Parser();
                parser.parse("plus=: +");
                parser.parse("plusx=: plus");
                var z =  parser.parse("1 plusx 2").ToString();
                return z == "3";
            };

            tests["rank conjunction /+\"1 i.3"] = () => equals(toWords("/+\"1 i.3"), new string[] { "/", "+", "\"", "1", "i.", "3" });
            
            foreach (var key in tests.Keys) {
                if (!tests[key]()) {
                    //throw new ApplicationException(key);
                    Console.WriteLine("TEST " + key + " failed");
                }
            }

            Action<object, object> pair = (x,y) => {
                if (x == null) { throw new ApplicationException("input returned null"); }
                if (x.ToString()!=y.ToString()) {
                    throw new ApplicationException(String.Format("{0} != {1}", x,y));
                }
            };
            var eqTests = new Dictionary<string, Action>();
            
            eqTests["add"] = () => pair(parse("1 + 2").ToString(), "3");
            eqTests["add float"] = () => pair(parse("1.5 + 2.5").ToString(), "4");
            eqTests["add float + int"] = () => pair(parse("4.5 + 3").ToString(), "7.5");

            eqTests["subtract"] = () => pair(parse("4 - 3").ToString(), "1");
            eqTests["subtract float - int"] = () => pair(parse("4.5 - 3").ToString(), "1.5");
            eqTests["subtract int - float"] = () => pair(parse("4 - 3.5").ToString(), "0.5");

            eqTests["subtract negative"] = () => pair(parse("5 - 6").ToString(), "_1");

            eqTests["multiply int"] = () => pair(parse("2 * 3").ToString(), "6");
            eqTests["multiply float"] = () => pair(parse("2.5 * 2.5").ToString(), "6.25");
            eqTests["multiply int*float"] = () => pair(parse("2 * 2.5").ToString(), "5");

            eqTests["divide int"] = () => pair(parse("10 % 2").ToString(), "5");
            eqTests["divide float"] = () => pair(parse("1 % 4").ToString(), "0.25");

            eqTests["iota simple"] = () => pair(parse("i. 3").ToString(), "0 1 2");
            eqTests["iota simple negative"] = () => pair(parse("i. _3").ToString(), "2 1 0");
            eqTests["iota negative array"] = () => pair(parse("i. _2 _2").ToString(), "3 2\n1 0");
            //todo failing
            //eqTests["iota negative mixed"] = () => pair(parse("i. 3 _3"), "2 1 0\n5 4 3\n8 7 6");
            eqTests["shape iota simple"] = () => pair(parse("$ i. 3").ToString(), "3");

            eqTests["reshape int"] = () => pair(parse("3 $ 3").ToString(),"3 3 3");
            eqTests["reshape int"] = () => pair(parse("2 3 $ 3").ToString(),"3 3 3\n3 3 3");
            eqTests["reshape double"] = () => pair(parse("3 $ 3.2").ToString(),"3.2 3.2 3.2");
            eqTests["reshape string"] = () => pair(parse("3 2 $ 'abc'").ToString(),"ab\nca\nbc");

            
            eqTests["adverb simple"] = () => pair(parse("+/ i. 4").ToString(), "6");
            eqTests["multi-dimensional sum"] = () => pair(parse("+/ i. 2 3").ToString(),"3 5 7");
            eqTests["multi-dimensional"] = () => pair(parse("i. 2 3").ToString(),"0 1 2\n3 4 5");
            eqTests["multi-dimensional 2"] = () => pair(parse("i. 2 2 2").ToString(),"0 1\n2 3\n\n4 5\n6 7");
            eqTests["multi-dimensional add "] = () => pair(parse("1 + i. 2 2").ToString(),"1 2\n3 4");
            eqTests["multi-dimensional sum"] = () => pair(parse("+/ i. 2 3").ToString(),"3 5 7");
            eqTests["multi-dimensional sum higher rank"] = () => pair(parse("+/ i. 2 2 2").ToString(),"4 6\n8 10");
            eqTests["multi-dimensional sum higher rank 2"] = () => pair(parse("+/ i. 4 3 2").ToString(),"36 40\n44 48\n52 56");
            eqTests["assignment"] = () => pair(parse("a + a=:5").ToString(),"10");
            eqTests["*/ int"] = () => pair(parse("*/ 2 2 2").ToString(),"8");

            eqTests["+/ 2.5 2.5"] = () => pair(parse("+/ 2.5 2.5").ToString(),"5");
            
            eqTests["transpose"] = () => pair(parse("|: i. 2 3"),"0 3\n1 4\n2 5");
            eqTests["equals true"] = () => pair(parse("3 = 3"), "1");
            eqTests["equals false"] = () => pair(parse("3 = 2"), "0");
            eqTests["equals float false"] = () => pair(parse("3.2 = 2.2"), "0");
            eqTests["equals float true"] = () => pair(parse("3.2 = 3.2"), "1");
            eqTests["equals string"] = () => pair(parse("'abc' = 'abc'"), "1");
            eqTests["equals string false"] = () => pair(parse("'abc' = 'abb'"), "0");
            eqTests["equals array"] = () => pair(parse("( 0 1 2 ) = i. 3"), "1 1 1");
            eqTests["equals array false"] = () => pair(parse("( 0 1 3 ) = i. 3"), "1 1 0");

            eqTests["1 $ 'abc'"] = () => pair(parse("1 $ 'abc'"), "a");
            eqTests["shape empty - $ ''"] = () => pair(parse("$ ''"), "0");
            eqTests["shape string $ 2 2 $ 'abcd'"] = () => pair(parse("$ 2 2 $ 'abcd'"), "2 2");

            eqTests["tally"] = () => pair(parse("# 1"), "1");
            eqTests["tally i."] = () => pair(parse("# i. 5"), "5");
            eqTests["tally multidimensional"] = () => pair(parse("# i. 5 4 3"), "5");
            eqTests["tally empty"] = () => pair(parse("# 0 $ 0"), "0");

            eqTests["negative numbers add"] = () => pair(parse(" 1 + _5 _6"), "_4 _5");

            // should evaluate right to left, not left to right
            eqTests["$/ 1 1 5"] = () => pair(parse("$/ 1 1 5").ToString(),"5");
            eqTests["$/ 5 1 1"] = () => pair(parse("$/ 5 1 1").ToString(),"1 1 1 1 1");

            eqTests["*/ 1 + i. 3"] = () => pair(parse("*/ ( 1 + i. 3 )").ToString(),"6");

            eqTests["4 $ 'ab'"] = () => pair(parse("4 $ 'ab'").ToString(),"abab");

            eqTests["0%0'"] = () => pair(parse("0%0").ToString(),"0");
            eqTests["1%0"] = () => pair(parse("1%0").ToString(),"_");

            eqTests["array + array"] = () => pair(parse("a+a=: i. 2 2"),"0 2\n4 6");
            eqTests["dyadic adverb call"] = () => pair(parse("2 4 +/ 1 3"),"3 5\n5 7");

            eqTests["copy 5 # 3"] = () => pair(parse("5 # 3"),"3 3 3 3 3");

            eqTests["divide array"] = () => pair(parse("%/ (3 2 $ 1 1 5 5)"), "0.2 0.2");
            //failing for now
            //eqTests["copy 3 # i. 1 2"] = () => pair(parse("3 # i. 1 2"),"0 1\n0 1\n0 1");
            //eqTests["copy string"] = () => pair(parse("3 # 'ab'"),"aaabbbccc");

            eqTests["rank shape full"] = () => pair(parse("$\"3 i. 3 2 1"), "3 2 1");
            eqTests["rank shape full 1"] = () => pair(parse("$ $\"3 i. 3 2 1"), "3");
            eqTests["rank shape - rank-1"] = () => pair(parse("$\"2 i. 3 2 1"), "2 1\n2 1\n2 1");
            eqTests["rank shape - rank-2"] = () => pair(parse("$\"1 i. 3 2 1"), "1\n1\n\n1\n1\n\n1\n1");
            
            eqTests["rank shape - 1"] = () => pair(parse("$ $\"2 i. 3 2 1"), "3 2");
            eqTests["rank shape - 2"] = () => pair(parse("$ $\"1 i. 3 2 1"), "3 2 1");
            eqTests["rank shape tally"] = () => pair(parse("$ #\"1 i. 3 2 1"), "3 2");

            eqTests["conjunction with adverb +/"] = () => pair(parse("+/\"1 i. 3 3"), "3 12 21");
            
            
            foreach (var key in eqTests.Keys) {
                try {
                    eqTests[key]();
                } catch (Exception e) {
                    Console.WriteLine("TEST " + key + " Exception:\n" + e.ToString());
                }
            }
        }
    }
}
