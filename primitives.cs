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
using System.Numerics;
using System.Web.Script.Serialization;

#if CSSCRIPT
using CSScriptLibrary;
#endif

using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace MicroJ {
   
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

        public AType InvokeWithRank(AType y) {
            if (Monadic == 0 && !y.IsAtom()) {
                AType[] ret = new AType[y.GetCount()];

                for (var i = 0; i < ret.Length; i++) {
                    ret[i] = MonadicFunc(y.GetValA(i));
                }
                return ret[0].Merge(new long[] { ret.Length }, ret);
            }
            else {
                return MonadicFunc(y);
            }
        }
    }
   
    public class Verbs {

        public static readonly string[] Words = new[] { "+", "-", "*", "%", "i.", "$", "#", "=", "|:", 
            "|.", "-:", "[", "p:", ",", "!", ";", "q:", "{." , "}.", 
            "<", "<:", "<.",  ">",">:", ">." , "{", "/:", "\\:", "*:", "+:", "\":", "~.", ",.", "]", "[:", "}:", "I.", "|", ";:", "+.", "E.", "~:", "*."};

        public static readonly string[] ControlWords = new[] { "if.", "end.", "do.", "else.", "elseif.", "while." };

        public Adverbs Adverbs = null;
        public Conjunctions Conjunctions = null;

        //Func<A<long>, A<JString>, A<JString>> copyFunc;
        //Delegate copyFunc;

        public class SpecialCodeEval {
            public Func<AType, AType> monad;
            public Func<AType, AType, AType> dyad;
            public Func<AType, bool> evalType;
            public Func<AType, AType, bool> evalTypeDyad;
        }
        Dictionary<Tuple<string, Type, Type>, Delegate> expressionDict;
        public Dictionary<string, SpecialCodeEval> SpecialCode = new Dictionary<string,SpecialCodeEval>();
        

        public Dictionary<string, VerbWithRank> expressionMap;
        public Verbs() {
            expressionDict = new Dictionary<Tuple<string, Type, Type>, Delegate>();
            expressionMap = new Dictionary<string, VerbWithRank>();
            expressionMap["p:"] = new VerbWithRank(primesm, primes, 0, VerbWithRank.Infinite, VerbWithRank.Infinite);
            expressionMap["q:"] = new VerbWithRank(primesqm, primesq, 0, 0, 0);
            expressionMap[";:"] = new VerbWithRank((y) => {
                var words = Conjunctions.Parser.toWords(y.GetString(0));
                return new A<Box>(words.Length) { Ravel = words.Select(xv => new JString { str = xv }.WrapA().Box()).ToArray() };
            }, null, 0, 0, 0);

            //determine nameClass
            expressionMap["nameClass"] = new VerbWithRank((y) => {
                var str = y.GetValA(0).UnBox().ToString();
                var val = AType.MakeA(str, Conjunctions.Parser.Names, Conjunctions.Parser.LocalNames);
                
                if (Adverbs.Words.Contains(str)) {
                    return AType.MakeA(1);
                } 
                else if (Conjunctions.Words.Contains(str)) {
                    return AType.MakeA(2);
                } 
                else if (val.GetType() == typeof(A<Verb>)) {
                    return AType.MakeA(3);
                }
                else if (Verbs.ControlWords.Contains(str)) {
                    return AType.MakeA(4);
                }
                else if (val.GetType() == typeof(A<Undefined>)) {
                    return AType.MakeA(-1);
                }                
                else  {
                    return AType.MakeA(0);
                } 
            }, null, 0, 0, 0);
            

            SpecialCode["([:,/({.,.}.))\"1"] = new SpecialCodeEval {
                evalType = (y) => {
                    return y.GetType() == typeof(A<JTable>);
                },
                monad = this.unpivotTable
            };
            SpecialCode["+/"] = new SpecialCodeEval {
                evalType = (y) => {
                    //special code path only works on rank 0/1
                    if (y.Rank > 1) { return false;  }
                    return true;
                },
                monad = y => {
                    switch (y.TypeE) {
                        case AType.Types.Long: return Adverbs.reduceplus(y as A<long>);
                        case AType.Types.Double: return Adverbs.reduceplus(y as A<double>);
                        case AType.Types.Decimal: return Adverbs.reduceplus(y as A<decimal>);
                    }
                    throw new NotImplementedException();
                }
            };
            SpecialCode["+/\\"] = new SpecialCodeEval {
                evalType = (y) => {
                    //special code path only works on rank 0/1
                    if (y.Rank > 1) { return false; }
                    return true;
                },
                monad = y => {
                    switch (y.TypeE) {
                        case AType.Types.Long: return Adverbs.runsum(y as A<long>);
                        case AType.Types.Double: return Adverbs.runsum(y as A<double>);
                        case AType.Types.Decimal: return Adverbs.runsum(y as A<decimal>);
                    }
                    throw new NotImplementedException();
                }
            };
            SpecialCode["((%)^:0~:])\"0"] = new SpecialCodeEval {
                evalType = (y) => {
                    return true;
                },
                dyad = (x, y) => {
                    if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                        return math((A<decimal>)x, (A<decimal>)y, (a, b) => b != 0 ? a / b : 0);
                    }
                    else {
                        var a2 = x.ConvertDouble();
                        var b2 = y.ConvertDouble();
                        return math(a2, b2, (a, b) =>  b == 0 ? 0 : Math.Round(a / b, 6));
                    }
                }
            };

            //hack to support string partial matching
            SpecialCode["(>./)@E.~"] = new SpecialCodeEval {
                evalType = (y) => {
                    //special code path only works on rank 0/1
                    return true;
                },
                dyad = (x, y) => {
                    //todo figure out why TypeE doesn't work
                    if (y.GetType() == typeof(A<JString>) && x.GetType() == typeof(A<JString>)) {
                        var xv = (x as A<JString>);
                        if (x.Rank <= 1 && xv.Ravel.Length == 1) {
                            var xt = xv.GetString(0);
                            var yt = (y as A<JString>).GetString(0);
                            return AType.MakeA(xt.IndexOf(yt, StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0);
                        }
                        else {
                            var z = new A<long>(x.Shape[0]);
                            var yt = (y as A<JString>).GetString(0);
                            var xt = (x as A<JString>);
                            for (var i = 0; i < z.Count; i++) {
                                z.Ravel[i] = xt.GetString(i).IndexOf(yt, StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
                            }

                            return z;
                        }
                        
                    }                    
                    throw new NotImplementedException();
                }
            };
        }

        public AType InvokeExpression(string op, AType x, AType y, int generics, object callee = null, AType newVerb = null, bool tryConvertLong = true) {
            var key = new Tuple<string, Type, Type>(op, x.GetType(), y.GetType());
            Delegate d;

            if (tryConvertLong) {
                //no xs take decimals right now, so lets convert to long 
                x = x.TryConvertLong(Conjunctions.Parser);
            }
            
            if (!expressionDict.TryGetValue(key, out d)) {
                var calleeType = callee == null ? typeof(Verbs) : callee.GetType();

                MethodInfo meth;
                if (generics == 0) {
                    meth = calleeType.GetMethod(op);
                }
                else if (generics == 1) {
                    meth = calleeType.GetMethod(op).MakeGenericMethod(y.GetType().GetGenericArguments().First());
                }
                else {
                    meth = calleeType.GetMethod(op).MakeGenericMethod(x.GetType().GetGenericArguments().First(), y.GetType().GetGenericArguments().First());
                }

                if (newVerb == null) {
                    var par1 = Expression.Parameter(x.GetType());
                    var par2 = Expression.Parameter(y.GetType());
                    var me = callee == null ? this : callee;
                    var instance = Expression.Constant(me);
                    var call = Expression.Call(instance, meth, par1, par2);

                    d = Expression.Lambda(call, par1, par2).Compile();
                }
                else {
                    var par0 = Expression.Parameter(newVerb.GetType());
                    var par1 = Expression.Parameter(x.GetType());
                    var par2 = Expression.Parameter(y.GetType());
                    var me = callee == null ? this : callee;
                    var instance = Expression.Constant(me);
                    var call = Expression.Call(instance, meth, par0, par1, par2);

                    d = Expression.Lambda(call, par0, par1, par2).Compile();
                }
                
                expressionDict[key] = d;
            }
            if (newVerb == null) {
                return (AType)d.DynamicInvoke(x, y);
            }
            else {
                return (AType)d.DynamicInvoke(newVerb, x, y);
            }
            
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

        public A<T> iota<T>(A<T> y) where T : struct {
            var shape = y.Ravel.Select(x => Convert.ToInt64(x)).ToArray();
            var ascending = shape.All(x => x >= 0);
            long ct = prod(shape);
            var k = Math.Abs(ct);
            var z = new A<T>(k);
            if (y.Rank > 0) { z.Shape = shape.Select(x => Math.Abs(x)).ToArray(); }
            if (z.GetType() == typeof(A<long>))
                z.Ravel = permutationIdx(z.Shape).Cast<T>().ToArray();
            else
                z.Ravel = permutationIdx(z.Shape).Select(x=>Convert.ToDecimal(x)).Cast<T>().ToArray();

            if (!ascending) {
                for (var i = z.Rank - 1; i >= 0; i--) {
                    if (shape[i] < 0) {
                        var nr = z.Rank - i;
                        var conj = new A<Verb>(0);
                        conj.Ravel[0] = new Verb { op = "|.", conj = "\"", rhs = nr.ToString() };
                        z = (A<T>)Conjunctions.rank1ex<T>(conj, z);
                    }
                }
            }
            return z;
        }


        public A<T> math<T>(A<T> x, A<T> y, Func<T, T, T> op) where T : struct {

            if (x.Rank == 0 && y.Rank == 0) {
                var z = new A<T>(0);
                z.Ravel[0] = op(x.Ravel[0], y.Ravel[0]);
                return z;
            }
            if (x.Rank == 0) {
                var z = new A<T>(y.Ravel.Length, y.Shape);
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
                return z;
            }
            else if (y.Rank == 0 && x.Rank > 0) {
                var z = new A<T>(x.Ravel.Length, x.Shape);
                for (var i = 0; i < x.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[0]);
                }
                return z;
            }
            else {
                var z = new A<T>(y.Ravel.Length, y.Shape);
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
                return z;
            }
            throw new NotImplementedException();
        }

        //dynamic dispatch of math operations -- slowest, around 7x slower
        public A<double> mathmixed(dynamic x, dynamic y, Func<dynamic, dynamic, dynamic> op) {
            
            if (x.Rank == 0) {
                var z = new A<double>(y.Ravel.Length, y.Shape);
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[0], y.Ravel[i]);
                }
                return z;
            }
            else if (y.Rank == 0) {
                var z = new A<double>(x.Ravel.Length, x.Shape);
                for (var i = 0; i < x.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[0]);
                }
                return z;
            }
            else {
                var z = new A<double>(y.Ravel.Length, y.Shape);
                for (var i = 0; i < y.Ravel.Length; i++) {
                    z.Ravel[i] = op(x.Ravel[i], y.Ravel[i]);
                }
                return z;
            }            
        }

        //significantly faster than dispatch (10x)
        //0 [ arr=: i. 10000000
        //0.0871601 (6!:2) '1<arr' NB. same type
        //2.8003321 (6!:2) '1.0 <arr' NB. old
        //0.2403719 (6!:2) '1.0 <arr' NB. new
        public A<long> mathmixedLong(AType x, AType y, Func<long, long, long> op) {
            if (x.Rank == 0 && y.Rank == 0) {
                var z = new A<long>(0);
                z.Ravel[0] = op(x.GetLong(0), y.GetLong(0));
                return z;
            }
            if (x.Rank == 0) {
                var z = new A<long>(y.GetCount(), y.Shape);
                var xl = x.GetLong(0);
                for (var i = 0; i < z.Count; i++) {
                    z.Ravel[i] = op(xl, y.GetLong(i));
                }
                return z;
            }
            else if (y.Rank == 0 && x.Rank > 0) {
                var z = new A<long>(x.GetCount(), x.Shape);
                var yl = y.GetLong(0);
                for (var i = 0; i < z.Count; i++) {
                    z.Ravel[i] = op(x.GetLong(i), yl);
                }
                return z;
            }
            else {
                var z = new A<long>(y.GetCount(), y.Shape);
                for (var i = 0; i < z.Count; i++) {
                    z.Ravel[i] = op(x.GetLong(i), y.GetLong(i));
                }
                return z;
            }
            throw new NotImplementedException();
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
            if (y.Rank == 0) { v.Ravel = null; }
            else { v.Ravel = y.Shape; }
            return v;
        }

        public A<long> tally(AType y) {
            var v = new A<long>(0);
            if (y.GetType() == typeof(A<JTable>)) { v.Ravel[0] = (y as A<JTable>).First().RowCount;  }
            else if (y.Rank <= 1 && y.GetType() == typeof(A<JString>) && y.GetCount() == 1) { v.Ravel[0] = y.GetString(0).Length; }
            else if (y.Rank <= 1 && y.GetType() == typeof(A<JString>) && y.GetCount() > 1) {
                var vl = new A<long>(y.GetCount());
                for (var i = 0; i < vl.Count; i++) {
                    vl.Ravel[i] = y.GetString(i).Length;
                }
                return vl;
            }
            else if (y.Rank == 0) { v.Ravel[0] = 1; }
            else { v.Ravel[0] = y.Shape[0]; }
            return v;
        }


        public A<Box> box(AType y) {
            var v = new A<Box>(0);
            v.Ravel[0] = new Box { val = y };
            return v;
        }

        public AType unbox(A<Box> y) {
            if (y.Shape == null) {
                return y.Ravel[0].val;
            }
            var newShape = y.ShapeCopy();
            var shape = y.Ravel[0].val.Shape;
            if (shape == null) {
                shape = new long[] { 1 };
            }
            newShape = newShape.Concat(shape).ToArray();
            var v = y.Ravel[0].val.Merge(newShape, y.Ravel.Select(x => x.val).ToArray());

            return v;
        }


        public A<T> copy<T>(A<long> x, A<T> y) where T : struct {
            if (x.Rank == 0) {
                var copies = x.Ravel[0];
                var ct = copies * y.Count;
                var shape = y.Shape != null ? y.ShapeCopy() : new long[] { 1 };
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
            else if (x.Rank == 1 && x.Shape[0] == y.Shape[0]) {
                var ct = x.Ravel.Where(xx => xx > 0).Sum();
                var subshape = y.Shape.Skip(1).ToArray();
                var subshapect = AType.ShapeProduct(subshape);
                var shape = new long[] { ct }.Concat(subshape).ToArray();

                var v = new A<T>(shape);
                long offset = 0;
                if (y.GetType() != typeof(A<JString>)) {
                    for(var xi = 0; xi < x.Count; xi++) {                    
                        for (var i = 0; i < x.Ravel[xi]; i++) {
                            for (var k = 0; k < subshapect; k++) {
                                v.Ravel[offset++] = y.Ravel[(xi*subshapect)+k];
                            }
                        }
                    }
                    
                } else {
                    for(var xi = 0; xi < x.Count; xi++) {                    
                        for (var i = 0; i < x.Ravel[xi]; i++) {
                            v.Ravel[offset++] = y.Ravel[xi];
                        }
                    }
                }
                return v;
            }
            else {
                throw new NotImplementedException();
            }
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

            var maxCt = y.Count > x.Count ? y.Count : x.Count;
            var z = new A<bool>(maxCt);

            if (x.GetType() == y.GetType() && x.GetType() != typeof(A<JString>)) {
                var yt = (y as A<T>);
                for (var i = 0; i < maxCt; i++) {
                    var xi = i < x.Count ? i : 0;
                    var yi = i < y.Count ? i : 0;
                    z.Ravel[i] = x.FastEquals(x.Ravel[xi], yt.Ravel[yi]);
                    
                }
            }
            else {
                for (var i = 0; i < maxCt; i++) {
                    //todo: need a faster way to compare equality, this was failing on double to long comparisons
                    //x.Ravel[i].Equals(y.Ravel[i]);
                    z.Ravel[i] = x.StringConverter(x.Ravel[i < x.Count ? i : 0]) == y.StringConverter(y.Ravel[i < y.Count ? i : 0]);
                }
            }
            return z;
        }

        public A<bool> notequals<T, T2>(A<T> x, A<T2> y) where T : struct where T2 : struct {
            //todo handle application errors without exceptions
            var maxCt = y.Count > x.Count ? y.Count : x.Count;
            var z = new A<bool>(maxCt);
            if (x.GetType() == y.GetType() && x.GetType() != typeof(A<JString>)) {
                var yt = (y as A<T>);
                for (var i = 0; i < maxCt; i++) {
                    var xi = i < x.Count ? i : 0;
                    var yi = i < y.Count ? i : 0;
                    z.Ravel[i] = !x.FastEquals(x.Ravel[xi], yt.Ravel[yi]);
                }
            }
            else {
                for (var i = 0; i < maxCt; i++) {
                    //todo: need a faster way to compare equality, this was failing on double to long comparisons
                    //x.Ravel[i].Equals(y.Ravel[i]);
                    z.Ravel[i] = x.StringConverter(x.Ravel[i < x.Count ? i : 0]) != y.StringConverter(y.Ravel[i < y.Count ? i : 0]);
                }
            }
            
            return z;
        }


        public A<JString> tostring<T>(A<T> y) where T: struct {
            if (y.Rank <= 1) {
                var str = y.ToString();
                var z = new A<JString>(new long[] { str.Length });
                z.Ravel[0] = new JString { str = str };
                return z;
            }
            else {
                var conj = new A<Verb>(0);
                conj.Ravel[0] = new Verb { op = "\":", conj = "\"", rhs = (y.Rank-1).ToString() };
                return (A<JString>)Conjunctions.rank1ex<T>(conj, y);
            }
            
            
        }

        public A<long> indexof<T>(A<T> x, A<T> y) where T : struct {            
            var z = new A<long>(y.Count);
            if (y.Rank <= 1 && x.Rank <= 1 && y.GetType() != typeof(A<Box>) && y.GetType() != typeof(A<JString>)) {
                for (var i = 0; i < y.Count; i++) {
                    z.Ravel[i] = x.Count;
                    for (var xi = 0; xi < x.Count; xi++) {
                        if (x.Ravel[xi].Equals(y.Ravel[i])) {
                            z.Ravel[i] = xi;
                            break;
                        }
                    }
                }
            }
            else {
                for (var i = 0; i < y.Count; i++) {
                    z.Ravel[i] = x.Count;
                    string yval = y.StringConverter(y.Ravel[i]);
                    for (var xi = 0; xi < x.Count; xi++) {
                        //todo: need a faster way to compare equality
                        if (x.StringConverter(x.Ravel[xi]) == yval) {
                            z.Ravel[i] = xi;
                            break;
                        }
                    }
                }
            }
            
            return z;
        }

        public A<T> ravel<T>(A<T> y) where T : struct {
            if (y.GetType() != typeof(A<JString>)) {
                var v = new A<T>(y.Count);
                for (var n = 0; n < y.Count; n++) {
                    v.Ravel[n] = y.Ravel[n];
                }
                return v;
            }
            else {
                
                var sb = new StringBuilder();
                for (var n = 0; n < y.Count; n++) {
                    sb.Append(y.GetString(n));
                }
                var str = sb.ToString();
                var v = new A<JString>(0);
                v.Ravel[0] = new JString { str = str };
                return (A<T>)(object)v;
            }            
        }

        public A<T> ravelitems<T>(A<T> y) where T : struct {
            if (y.Rank < 2) {
                var z = new A<T>(y.Count, y.Shape.Concat(new long[] { 1 }).ToArray());
                y.Ravel.CopyTo(z.Ravel, 0);
                return z;
            }
            else if (y.Rank == 2) {
                return y;
            }
            else {
                throw new NotImplementedException("Ravel Items not yet implemented on > 2 rank");
            }
        }


        public A<long> floor<T>(AType y) where T : struct {            
            if (y.GetType() == typeof(A<long>)) {
                A<long> ay = (A<long>)y;
                var v = new A<long>(ay.Count, ay.Shape);                
                for (var n = 0; n < v.Count; n++) {
                    v.Ravel[n] = ay.Ravel[n];
                }
                return v;
            }
            else if (y.GetType() == typeof(A<double>)) {
                A<double> ay = (A<double>)y;
                var v = new A<long>(ay.Count, ay.Shape);                
                for (var n = 0; n < v.Count; n++) {
                    v.Ravel[n] = (long)Math.Floor(ay.Ravel[n]);
                }
                return v;
            }
            else {
                throw new DomainException();
            }
        }

        public A<long> ceiling<T>(AType y) where T : struct {                        
            if (y.GetType() == typeof(A<long>)) {
                A<long> ay = (A<long>)y;
                var v = new A<long>(ay.Count, ay.Shape);
                for (var n = 0; n < v.Count; n++) {
                    v.Ravel[n] = ay.Ravel[n];
                }
                return v;
            }
            else if (y.GetType() == typeof(A<double>)) {
                A<double> ay = (A<double>)y;
                var v = new A<long>(ay.Count, ay.Shape);
                for (var n = 0; n < v.Count; n++) {
                    v.Ravel[n] = (long)Math.Ceiling(ay.Ravel[n]);
                }
                return v;
            }
            else {
                throw new DomainException();
            }
        }

        public A<T> head<T>(A<T> y) where T : struct {
            if (y.Shape == null) {
                var v = new A<T>(0);
                v.Ravel = y.Copy(1);
                return v;
            }
            //drop 1 from the shape;
            var z = from(new A<long>(0) { Ravel = new long[] { 0 } }, y);

            return z;
        }

        public A<T> behead<T>(A<T> y) where T : struct {
            long[] newShape = y.ShapeCopy();
            newShape[0] = newShape[0] - 1;           
            var v = new A<T>(newShape);
            if (newShape[0] > 0)
                v.Ravel = y.Copy(v.Count > 0 ? v.Count : 1, skip: y.ShapeProduct(skip: 1));
            return v;
        }


        public AType beheadTable(A<JTable> y) {
            var yv = y.First();
            if (yv.Columns.Length > 1) {
                var v = new A<Box>(yv.Columns.Length);
                if (yv.indices == null) {
                    v.Ravel = yv.Rows.Select(x => x).ToArray();
                }
                else {
                    v.Ravel = yv.Rows.Select(x=>x.val.FromIndices(yv.indices).Box()).ToArray();
                }
                return v;
            }
            else {
                if (yv.indices == null) {
                    return yv.Rows[0].val;
                }
                else {
                    return yv.Rows[0].val.FromIndices(yv.indices);                    
                }
            }            
        }


        
        public A<T> curtail<T>(A<T> y) where T : struct {
            long[] newShape = y.ShapeCopy();
            newShape[0] = newShape[0] - 1;
            var v = new A<T>(newShape);
            v.Ravel = y.Copy(v.Count > 0 ? v.Count : 1);
            return v;
        }

        
        public A<T> take<T>(A<long> x, A<T> y) where T : struct {
            long[] newShape = null;
            
            if (x.Rank > 0) { throw new NotImplementedException("Rank > 0 not implemented on take"); }
            var xct = Math.Abs(x.Ravel[0]);
            if (y.Shape != null) { newShape = new long[] { xct }.Concat(y.Shape.Skip(1)).ToArray(); }
            var v = new A<T>(newShape);

            if (y.GetType() == typeof(A<JString>) && y.Rank < 2) {
                var idx = (int)x.Ravel[0];
                if (idx >= 0) {
                    v.Ravel[0] = (T)(object)new JString { str = ((A<JString>)(object)y).Ravel[0].str.Substring(0, idx) };
                }
                else {
                    var str = ((A<JString>)(object)y).Ravel[0].str;
                    v.Ravel[0] = (T)(object)new JString { str = str.Substring(str.Length+idx) };
                }
                
            }
            else if (y.GetType() == typeof(A<JTable>)) {
                //todo: move to own
                var yt = (y as A<JTable>).First();
                var take = xct;
                var offset = 0L;
                var xv = x.Ravel[0];
                if (xv < 0) {
                    var rowCt = yt.Rows[0].val.Shape[0];
                    offset = xv +  rowCt;
                    take = rowCt - offset;
                    var zt = yt.Clone();
                    zt.take = take;
                    zt.offset = offset;
                    return (A<T>)(object)zt.WrapA();           
                }
                else {
                    take = xct;
                    long[] indices;
                    if (yt.RowCount == 0) {
                        indices = new long[0];
                    }
                    else if (yt.indices != null) { indices = yt.indices.Take((int)take).ToArray();  }
                    else {
                        indices = permutationIdx(new long[] { take }, true);
                    }
                    var zt = yt.Clone();
                    zt.indices = indices;
                    return (A<T>)(object)zt.WrapA(); 
                }
                    
            }
            else {
                v.Ravel = y.Copy(v.Count, ascending: x.Ravel[0] >= 0);
            }
            
            return v;
        }


        public A<T> drop<T>(A<long> x, A<T> y) where T : struct {

            long[] newShape = null;

            if (x.Rank > 0) { throw new NotImplementedException("Rank > 0 not implemented on drop"); }
            var xct = Math.Abs(x.Ravel[0]);
            if (y.Shape != null) { 
                newShape = y.Shape;
                newShape[0] -= xct;
            }
            var v = new A<T>(newShape);
            var skip = y.Count- AType.ShapeProduct(newShape);

            if (y.GetType() == typeof(A<JString>) && y.Rank < 2) {
                v.Ravel[0] = (T)(object)new JString { str = ((A<JString>)(object)y).Ravel[0].str.Substring((int)x.Ravel[0]) };
            }
            else {
                v.Ravel = y.Copy(v.Count, skip: skip, ascending: x.Ravel[0] >= 0);
            }
            
            return v;
        }
        
        public A<T> from<T>(A<long> x, A<T> y) where T : struct {
            long[] newShape = null;

            if (x.Rank > 1) { throw new NotImplementedException("Rank > 0 not implemented on from"); }
            var xct = Math.Abs(x.Ravel[0]);
            if (y.Shape != null) {
                newShape = y.ShapeCopy();
                newShape[0] = x.Count;
            }
            long subshapeCt = y.ShapeProduct(skip: 1);

            //todo hack for strings, which have 1 atom of the string (not array of chars)
            if (y.GetType() == typeof(A<JString>)) {
                subshapeCt = 1;
            }
            A<T> v;
            if (y.Rank == 1 && x.Count == 1) {
                v = new A<T>(0);
            }
            else if (y.Rank == 2 && x.Count == 1) {
                newShape = newShape.Skip(1).ToArray();
                v = new A<T>(newShape);
            }
            else {
                v = new A<T>(newShape);
            }
            
            long offset = 0;            
            foreach (var xv in x.Ravel) {
                bool ascending = xv >= 0;
                long skip = Math.Abs(xv) * subshapeCt;
                long yoffset = ascending ? skip : (y.Count - skip );
                for (long i = 0; i < subshapeCt; i++) {
                    v.Ravel[offset++] = y.Ravel[yoffset + i];
                }          
            }
            return v;
        }

        public A<JTable> fromtable<T, T2>(A<T> x, A<T2> y) where T : struct where T2 : struct {            
            if (x.Rank > 1) { throw new NotImplementedException("Rank > 0 not implemented on from"); }
            var yt = (y as A<JTable>).First();
            
            if (x.GetType() == typeof(A<Box>)) {
                var v = yt.Clone();
                var xb = x as A<Box>;
                
                //searching for a key requires a single boxed value <<'foo' or <<1
                if ((yt.UniqueKeys != null || yt.Key != null) && xb.First().val.GetType() == typeof(A<Box>) && xb.First().val.GetCount() == 1) {                    
                    if (yt.UniqueKeys != null) {
                        long idx;
                        if (yt.UniqueKeys.TryGetValue((xb.First().val as A<Box>).Ravel[0].val.ToString().ToUpper(), out idx)) {
                            v.indices = new long[] { idx };
                        }
                        else {
                            v.indices = new long[0];
                        }
                        return v.WrapA();
                    }
                    else if (yt.Key != null) {
                        List<long> indices;
                        if (yt.Key.TryGetValue((xb.First().val as A<Box>).Ravel[0].val.ToString().ToUpper(), out indices)) {
                            v.indices = indices.ToArray();
                        } else {
                            v.indices = new long[0];
                        }
                        return v.WrapA();
                    }
                    
                    
                }
                else {
                    var xbt = (x as A<Box>);
                    var idx = xbt.Ravel.Select(xv => yt.GetColIndex(xv.val)).ToArray();
                    var columns = new List<string>();
                    var rows = new List<Box>();
                    
                    
                    for(var k = 0; k < idx.Length; k++) {
                        var i = idx[k];
                        
                        //not boxed
                        if (i != -2) {
                            columns.Add(v.Columns[i]);
                            rows.Add(v.Rows[i]);
                        }
                        else {
                            //(<(<'c';'a+b')) {. (flip ('a';'b');(i.3);(3 $ 1 2))
                            var col = (xbt.Ravel[k].val as A<Box>);
                            var colName = (col.Ravel[0].val.GetString(0));

                            
                            var expression = col.Ravel[1].val.GetString(0);
                            //var expressionResult = Conjunctions.Parser.exec(expression, locals);
                            var expressionResult = Conjunctions.rank1table(new A<Verb>(1) { Ravel = new Verb[] { new Verb { explicitDef = expression } } }, (y as A<JTable>));


                            columns.Add(colName);
                            rows.Add(expressionResult.Box());
                        }                        
                    }
                    v.Columns = columns.ToArray();
                    v.Rows = rows.ToArray();
                    return v.WrapA();
                }                
            }
            else if (x.GetType() == typeof(A<long>) && (x.Rank == 1 || x.Rank == 0)) {
                var rowIndices = (x as A<long>).Ravel;
                var v = yt.Clone();
                v.indices = v.indices == null ? rowIndices : rowIndices.Select(xv => v.indices[xv]).ToArray();
                return v.WrapA();
            }
            else if (x.GetType() == typeof(A<JString>) && x.Rank < 2) {
                var locals = new Dictionary<string, AType>();
                var yv = (y as A<JTable>).First();
                if (Conjunctions.Parser.LocalNames != null) {
                    foreach (var kvx in Conjunctions.Parser.LocalNames) {
                        locals[kvx.Key] = kvx.Value;
                    }
                }

                for (var i = 0; i < yv.Columns.Length; i++) {
                    locals[JTable.SafeColumnName(yv.Columns[i])] = yv.indices == null ? yv.Rows[i].val : yv.Rows[i].val.FromIndices(yv.indices);
                }
                var expression = (x as A<JString>).First().str;
                var expressionResult = Conjunctions.Parser.exec(expression, locals);

                var expressionResultl = expressionResult as A<long>;
                var expressionResultb =  expressionResult as A<bool>;
                var expressionResultd = expressionResult as A<Decimal>;

                var v = yt.Clone();
                var indices = new List<long>();
                for (var i = 0; i < expressionResult.GetCount(); i++) {
                    if ((expressionResultl != null && expressionResultl.Ravel[i] == 1) || (expressionResultd != null && expressionResultd.Ravel[i] == 1) || (expressionResultb != null && expressionResultb.Ravel[i])) {
                        indices.Add(yv.indices != null ? yv.indices[i] : i);
                    }
                }
                v.indices = indices.ToArray();
                return v.WrapA();
            }
            throw new NotImplementedException();
        }

        //return indices of nub
        public Dictionary<long, List<long>> NubIndex<T>(A<T> x)  where T : struct {
            var frame = x.Shape.Skip(1).ToArray();
            var frameCt = AType.ShapeProduct(frame);            
            var n = x.Count / frameCt;

            bool isString = false;
            if (x.GetType() == typeof(A<JString>)) {
                 n = x.ShapeCopy()[0];
                 isString = true;
            }
            
            var indices = new Dictionary<long, List<long>>();
            if (isString) {
                var ax = (A<JString>) (object) x;
                var found = new Dictionary<string, List<long>>();
                for (var i = 0; i < n; i++) {
                    var key = ax.Ravel[i].str;
                    List<long> spot = null;
                    if (found.TryGetValue(key, out spot)) {
                        spot.Add(i);
                    }
                    else {
                        spot = new List<long>();
                        spot.Add(i);
                        found[key] = spot;
                    }
                }
                foreach (var val in found.Values) {
                    indices[val[0]] = val;
                }
            }
            else if (x.Rank == 1) {                 
                for (var i = 0; i < n; i++) {
                    var key = x.GetHashCode(i);
                    List<long> spot = null;
                    if (indices.TryGetValue(key, out spot)) {
                        spot.Add(i);
                    }
                    else {
                        spot = new List<long>();
                        spot.Add(i);
                        indices[key] = spot;
                    }
                }
            }
            else if (x.Rank == 2 && x.GetType() == typeof(A<Byte>)) {
                var shape = x.ShapeCopy();
                var newShape = shape.Take(shape.Length - 1).ToArray();
                var shapeProduct = AType.ShapeProduct(newShape);
                long offset = 0;
                long count = shape[shape.Length - 1];
                var ravel = (Byte[])(object)x.Ravel;
                for (var i = 0; i < shapeProduct; i++) {
                    var str = System.Text.Encoding.UTF8.GetString(ravel, (int)offset, (int)count);
                    var key = str.GetHashCode();
                    List<long> spot = null;
                    if (indices.TryGetValue(key, out spot)) {
                        spot.Add(i);
                    }
                    else {
                        spot = new List<long>();
                        spot.Add(i);
                        indices[key] = spot;
                    }
                    offset += count;
                }
            }
            else {
                for (var i = 0; i < n; i++) {
                    bool found = false;
                    foreach (var k in indices.Keys) {
                        if (x.SliceEquals((k * frameCt), (i * frameCt), frameCt)) {
                            indices[k].Add(i);
                            found = true;
                        }
                    }
                    if (!found) {
                        indices[i] = new List<long>();
                        indices[i].Add(i);
                    }
                }
            }
            return indices;
        }
        public A<long> gradeup<T>(A<T> y) where T : struct {
            if (y.Rank > 1 && y.GetType() != typeof(A<JString>)) {
                throw new NotImplementedException("Grade is not implemented on rank > 1");                
            }
            long[] indices;
            indices = y.GradeUp();
            A<long> ret = new A<long>(y.Count);
            for (long i = 0; i < y.Count; i++) {
                ret.Ravel[i] = indices[i];
            }
            return ret;
        }

        public A<long> gradedown<T>(A<T> y) where T : struct {
            var z = gradeup(y);
            z.Ravel = z.Ravel.Reverse().ToArray();
            return z;
        }

        public A<T> sortTable<T2, T>(A<T2> x, A<T> y, string gradeFunc)
            where T : struct
            where T2 : struct {
            var yt = (y as A<JTable>).First();
            var indices = InvokeExpression(gradeFunc, yt.Rows[yt.GetColIndex(x)].val) as A<long>;
            var z = new A<JTable>(1) { Ravel = new JTable[] { yt.Clone() } };
            if (yt.indices != null) {
                var ixHash = new HashSet<long>();
                ixHash.UnionWith(yt.indices);
                indices.Ravel = indices.Ravel.Where(xv => ixHash.Contains(xv)).ToArray();
            }

            z.Ravel[0].indices = indices.Ravel;
            return (A<T>)(object)z;
        }

        public A<T2> sortup<T2, T>(A<T2> x, A<T> y) where T : struct where T2 : struct {
            var indices = gradeup(y);
            return from(indices, x);            
        }

        public A<T> sortdown<T2, T>(A<T2> x, A<T> y) where T : struct where T2 : struct {
            if (y.GetType() == typeof(A<JTable>)) {
                var yt = (y as A<JTable>).First();
                var indices = InvokeExpression("gradedown", yt.Rows[yt.GetColIndex(x)].val) as A<long>; ;
                var z = new A<JTable>(1) { Ravel = new JTable[] { yt.Clone() } };
                if (yt.indices != null) {
                    var ixHash = new HashSet<long>();
                    ixHash.UnionWith(yt.indices);
                    indices.Ravel = indices.Ravel.Where(xv => ixHash.Contains(xv)).ToArray();
                }
                z.Ravel[0].indices = (indices as A<long>).Ravel;
                return (A<T>)(object)z;
            }
            else {
                var indices = gradedown(x);
                return from(indices, y);
            }
            
        }

        public AType raze<T>(A<Box> y) where T : struct {
            Type type = null;
            long totalCount = 0;
            for (var i = 0; i < y.Count; i++) {
                var thisType = y.Ravel[i].val.GetType();
                if (type != null && type != thisType) {
                    throw new DomainException();
                }
                if (y.Ravel[i].val.Shape != null) {
                    totalCount += AType.ShapeProduct(y.Ravel[i].val.Shape);
                }
                else {
                    totalCount += 1;
                }
                type = thisType;
            }
            if (type == typeof(A<long>)) {
                //e.g. ; < i. 3 3
                if (y.Count == 1) {
                    var z = new A<long>(totalCount, y.Ravel[0].val.Shape);
                    long offset = 0;
                    for (var i = 0; i < y.Count; i++) {
                        for (var k = 0; k < y.Ravel[i].val.GetCount(); k++) {
                            z.Ravel[offset++] = ((A<long>)y.Ravel[i].val).Ravel[k];
                        }
                    }
                    return z;
                }
                else {
                    var op = new A<Verb>(0);
                    op.Ravel[0] = new Verb { op = "," };
                    return Adverbs.reduceboxed<long>(op, y);
                }
            }
            else if (type == typeof(A<double>)) {
                var op = new A<Verb>(0);
                op.Ravel[0] = new Verb { op = "," };
                return Adverbs.reduceboxed<double>(op, y);
            }
            else if (type == typeof(A<decimal>)) {
                var op = new A<Verb>(0);
                op.Ravel[0] = new Verb { op = "," };
                return Adverbs.reduceboxed<decimal>(op, y);
            }            
            else if (type == typeof(A<JString>)) {
                var op = new A<Verb>(0);
                op.Ravel[0] = new Verb { op = "," };
                return Adverbs.reduceboxed<JString>(op, y);
            }            
            return null;
        }

        public A<T> append<T>(A<T> x, A<T> y) where T : struct {
            //if (x.Rank > 1 && AType.ShapeProduct(x.Shape) != AType.ShapeProduct(y.Shape)) throw new NotImplementedException("Rank > 1 non-equal shapes not implemented yet (need framing fill)");

            if (x.GetType() == typeof(A<JString>) && y.GetType() == typeof(A<JString>) && x.Rank <= 1 && y.Rank <= 1) {
                var vs = new A<JString>(0);
                vs.Ravel[0] = new JString { str = String.Intern(x.GetString(0) + y.GetString(0)) };
                return (A<T>)(object)vs;
            }
            long[] newShape;
            newShape = new long[] { x.Count + y.Count };

            if (y.Rank > 0) {
                var tail = y.Shape.Skip(1).ToArray();
                var xframe = x.Rank > 1 ? x.Shape[0] : 1;
                newShape = new long[] { xframe + y.Shape[0] }.Concat(tail).ToArray();
            }

            var v = new A<T>(x.Count + y.Count, newShape);
            var offset = 0;
            for (var n = 0; n < x.Count; n++) {
                v.Ravel[offset++] = x.Ravel[n];
            }
            for (var n = 0; n < y.Count; n++) {
                v.Ravel[offset++] = y.Ravel[n];
            }

            return v;
        }

        public A<Box> link<T2, T>(A<T2> x, A<T> y) where T : struct where T2 : struct {
          if (y.GetType() != typeof(A<Box>)) {
                var z = new A<Box>(2);
                z.Ravel[0] = new Box { val = x };            
                z.Ravel[1] = new Box { val = y };
                return z;
            } else {
                var z = new A<Box>(y.Count+1);
                z.Ravel[0] = new Box { val = x };
                for(var i = 0; i < y.Count; i++) {
                    z.Ravel[1+i] = (Box)(object)y.Ravel[i];
                }
                return z;
            }            
        }

        public A<JTable> linktable(A<JTable> x, A<JTable> y) {
            var xv = x.First();
            var yv = y.First();

            var matches = xv.Columns.Select((v, i) => new { xv = xv, xi = i, yi = Array.IndexOf(yv.Columns, v) });
            var z = xv.Clone();
            foreach (var match in matches.Where(v=>v.yi >= 0)) {
                z.Rows[match.xi] = yv.Rows[match.yi];
            }
            var found = matches.Where(v=>v.yi>=0).Select(v=>v.yi).ToArray();
            z.Columns = z.Columns.Concat(yv.Columns.Where((v,i)=>!found.Contains(i))).ToArray();
            z.Rows = z.Rows.Concat(yv.Rows.Where((v, i) => !found.Contains(i))).ToArray();

            return z.WrapA();
        }

        //links a table with a boxed set of columns and row expressions
        //example: (flip ('a';'b');((1,2);(3,5)));((<'c');'a+b'))) -: 0 : 0
        public A<JTable> linktableExpression(A<JTable> x, A<Box> y) {
            var xv = x.First();
            var z = xv.Clone();

            var newColumns = AHelper.ToOptions(y);
            //var columns = (y.Ravel[0].val as A<Box>).Ravel.Select(v => ((A<JString>)v.val).Ravel[0].str).ToArray();
            //var expressions = (y.Ravel[1].val as A<JString>).Ravel.Select(v => v.str).ToArray();
            var columns = newColumns.Keys.ToArray();
            var expressions = newColumns.Values.ToArray();

            //var matches = xv.Columns.Select((v, i) => new { xv = xv, xi = i, yi = Array.IndexOf(columns, v) }).ToArray();

            var matches = columns.Select((v, i) => new { col = v, colIdx = i }).ToArray();

            foreach(var match in matches) {
                var yt = new JTable {
                    Columns = new string[] { match.col }
                };
                var locals = new Dictionary<string, AType>();

                if (Conjunctions.Parser.LocalNames != null) {
                    foreach (var kvx in Conjunctions.Parser.LocalNames) {
                        locals[kvx.Key] = kvx.Value;
                    }
                }

                for (var i = 0; i < z.Columns.Length; i++) {
                    locals[JTable.SafeColumnName(z.Columns[i])] = z.Rows[i].val;
                }                
                var expressionResult = Conjunctions.Parser.exec(expressions[match.colIdx], locals);
                yt.Rows = new Box[] { new Box { val = expressionResult } };
                var zt = linktable(z.WrapA(), yt.WrapA());
                z = zt.Ravel[0];
            }
            z.ColumnExpressions = new Dictionary<string, string>();
            for (var k = 0; k < columns.Length; k++) {
                z.ColumnExpressions[columns[k]] = expressions[k];
            }
                return z.WrapA();
        }

        public A<T> nub<T>(A<T> y) where T : struct {            
            var indices = NubIndex(y);
            var fromIdx = new A<long>(indices.Count);
            fromIdx.Ravel = indices.Values.Select(x => x.First()).OrderBy(x=>x).ToArray();
            return from(fromIdx, y);    
        }

        public A<JTable> nubTable(A<JTable> y)  {
            var yt = y.First();
            var keys = new JString[yt.RowCount];

            if (yt.RowCount == 0) {
                var v = yt.Clone();
                v.indices = new long[0];
                return v.WrapA();
            }
            for (var i = 0; i < yt.RowCount; i++) {
                keys[i].str = "";
                for (var k = 0; k < yt.Rows.Length;k++) {
                    keys[i].str += yt.Rows[k].val.GetString(yt.indices == null ? i : yt.indices[i]);
                }
            }
            var indices = NubIndex(new A<JString>(yt.RowCount) { Ravel = keys });
            var z = yt.Clone();
            z.indices = indices.Values.Select(x => (yt.indices != null ? yt.indices[x.First()] : x.First())).OrderBy(x => x).ToArray();
            return z.WrapA();
        }

        public AType primesm(AType w) {
            return primes(null, w);
        }
        public AType primes(AType a, AType w) {
            var x = a != null ? (A<long>)a : new A<long>(0);
            var y = (A<long>)w;
            var xv = x.Ravel[0];
            Func<long, long> fl = null;
            Func<long, bool> fb = null;
            Func<long, long[]> fls = null;
            long[] frame = null;
            if (a == null) fl = Primes.GetNthPrime;
            else if (xv == -1) fl = (l) => Primes.Pi((float)(l-0.5f));
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

            else throw new NotImplementedException();
        }
        public AType primesqm(AType w) {
            return primesq(null, w);
        }
        public AType primesq(AType a, AType w) {
            var x = a != null ? (A<long>)a : new A<long>(0);
            var y = (A<long>)w;
            var xv = x.Ravel[0];
            Func<long, long> fl = null;
            Func<long, bool> fb = null;
            Func<long, long[]> fls = null;
            long[] frame = null;
            if (a == null) fls = (l) => Primes.Factor(l);
            else if ((long)xv <= Int32.MaxValue && xv > 0) {
                //  frame = new long[] { 2 };
                fls = (l) => {
                    Dictionary<long, int> facs = Primes.GetFactorExponents(Primes.Factor(l).ToList());
                    List<long> ps = Primes.atkinSieve(1 + (int)l); // first xv primes
                    List<long> pps = new List<long>(); //prime powers
                    int i = 0;
                    foreach (long p in ps) {
                        if (facs.ContainsKey(p)) {
                            pps.Add(facs[p]);
                        }
                        else pps.Add(0);
                        i++;
                        if (i >= (int)xv) break;
                    }
                    while (pps.Count < (int)xv)
                        pps.Add(0);
                    return pps.ToArray();
                };
            }

            if (fl != null) return y.Apply(fl);
            else if (fb != null) return y.Apply(fb);
            else if (fls != null) return y.Apply(fls, frame);

            else throw new NotImplementedException();
        }

        public A<T> stitch<T>(A<T> x, A<T> y) where T : struct {            
            if (x.Rank == 0 && y.Rank == 0) {                
                long[] newShape = new long[] { 2 };
                A<T> z = new A<T>(newShape);
                z.Ravel[0] = x.Ravel[0];
                z.Ravel[1] = y.Ravel[0];
                return z;
            }
            else if (x.Rank == 0 && y.Rank > 0) {
                var yshape = y.ShapeProduct(1);
                //make # of rows be 1-cells y and add room for x in cell
                long[] newShape = new long[] { y.Shape[0], 1 + yshape };
                A<T> z = new A<T>(newShape);

                var offset = 0;
                var yoffset = 0;
                for (var i = 0; i < newShape[0]; i++) {                    
                    for (var k = 0; k < newShape[1]; k++) {
                        if (k == 0)
                            z.Ravel[offset++] = x.Ravel[0];
                        else
                            z.Ravel[offset++] = y.Ravel[yoffset++];    
                    }
                }
                return z;
            }
            else {
                var xshape = x.ShapeProduct(1);
                var yshape = y.ShapeProduct(1);
                long[] newShape = new long[] { x.Shape[0], xshape + yshape };
                A<T> z = new A<T>(newShape);
                var xoffset = 0;
                var yoffset = 0;
                var zoffset = 0;
                for (var i = 0; i < newShape[0];i++) {
                    for (var k = 0; k < xshape; k++) {
                        z.Ravel[zoffset++] = x.Ravel[xoffset++];
                    }
                    for (var k = 0; k < yshape; k++) {
                        z.Ravel[zoffset++] = y.Ravel[yoffset++];
                    }
                }
                return z;
                
            }
        }

        //indexof I.
        public A<T> intervalIndex<T>(A<T> x, A<T> y) where T : struct {
            var par1 = Expression.Parameter(typeof(T));
            var par2 = Expression.Parameter(typeof(T));
            var lt = Expression.LessThan(par1, par2);

            var LessThan = Expression.Lambda<Func<T, T, bool>>(lt, par1, par2).Compile();
            Func<long, T> setter;

            //todo terrible boxing
            if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                setter = (a) => (T)(object)Convert.ToDecimal(a);
            }
            else {
                setter = (a) => (T)(object)a;
            }
                
            var z = new A<T>(y.Count);
            for(var i = 0; i < y.Count; i++) {
                for (var k = 0; k < x.Count; k++) {
                    if (LessThan(x.Ravel[k], y.Ravel[i])) {
                        z.Ravel[i] = setter(k+1);
                    }
                }

            }
            return z;
        }

        public A<JTable> copyTable(A<JTable> y) {
            var yt = y.First();
            var zt = new JTable {
                Columns = yt.Columns.Select(x => x).ToArray(),
                Rows = new Box[yt.Columns.Length]
            };
            
            for (var i = 0; i < zt.Columns.Length; i++) {
                if (yt.indices != null) {
                    zt.Rows[i].val = yt.Rows[i].val.FromIndices(yt.indices);
                }
                else {
                    zt.Rows[i].val = yt.Rows[i].val;
                }
                
            }
            return zt.WrapA();
            
                

        }
        public AType Call2(AType method, AType x, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            var verbs = (A<Verb>)method;


            SpecialCodeEval eval;
            var exp = verbs.ToString().Replace(" ", "");
            if (SpecialCode.TryGetValue(exp, out eval)) {
                if (((eval.evalType != null && eval.evalType(y)) || (eval.evalTypeDyad != null && eval.evalTypeDyad(x,y))) && eval.dyad != null) {
                    return eval.dyad(x,y);
                }
            }


            if (verb.explicitDef != null) {
                return runExplicit(verb.explicitDef, y, x);
            }

            if (verb.childVerb != null) {
                var v = verb.childVerb as A<Verb>;
                if (v != null) {
                    return Call2(v, x, y);
                }
            }
            if (verb.adverb != null) {
                return Adverbs.Call2(method, x, y);
            }

            if (verbs.GetCount() > 1) {
                
                AType z1 = null;
                //support noun in left tine
                Verb v = verbs.ToAtom(0).Ravel[0];
                if (v.rhs != null && v.op == null && v.childAdverb == null && v.conj == null) {
                    z1 = AType.MakeA(v.rhs, null);
                }
                else if (v.op == "[:") {
                    var m3 = Call2(verbs.ToAtom(2),x, y);
                    var m2 = Call1(verbs.ToAtom(1), m3);
                    return m2;
                }
                else {
                    z1 = Call2(verbs.ToAtom(0), x, y);
                }                
                var z3 = Call2(verbs.ToAtom(2), x, y);
                var z2 = Call2(verbs.ToAtom(1), z1, z3);
                return z2;
            } 

            //future: add check for integrated rank support
            if (verb.conj != null) {
                return Conjunctions.Call2(method, x, y);
            }

            var op = verb.op;

            VerbWithRank verbWithRank = null;

            if (op == "]") {
                return y;
            }
            else if (op == "[") {
                return x;
            }
            else if (op == "+") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a + b);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a + b);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a + b);
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
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a - b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a - b);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a - b);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a - b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a - b);
                }
            }
            else if (op == "*") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a * b);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a * b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    //add rounding to 6 decimal points
                    //TODO: make option
                    return math((A<double>)x, (A<double>)y, (a, b) => Math.Round(a * b,8));
                }
                else if (x.GetType() == typeof(A<bool>) && y.GetType() == typeof(A<bool>)) {
                    return math((A<bool>)x, (A<bool>)y, (a, b) => ((a ? 1 : 0) * (b ? 1 : 0)) == 1 ? true : false);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a * b);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a * b);
                }
                 
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a * b);
                }                                
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<bool>)) {
                    return mathmixed((A<double>)x, (A<bool>)y, (a, b) => (a * (b ? 1 : 0)));
                }                
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a * b);
                }
            }
            else if (op == "<") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.CanBeInt()  && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a < b ? 1 : 0);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a < b ? 1 : 0);
                }
            }
            else if (op == "<:") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a <= b ? 1 : 0);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a <= b ? 1 : 0);
                }
            }
            else if (op == ">") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a > b ? 1 : 0);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a > b ? 1 : 0);
                }
            }
            else if (op == ">:") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.GetType() == typeof(A<BigInteger>) && y.GetType() == typeof(A<BigInteger>)) {
                    return math((A<BigInteger>)x, (A<BigInteger>)y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a >= b ? 1 : 0);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a >= b ? 1 : 0);
                }
            }
            else if (op == "<.") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a < b ? a : b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a < b ? a : b);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a < b ? a : b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a < b ? a : b);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a < b ? a : b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a < b ? a : b);
                }
            }           
            else if (op == ">.") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>)) {
                    return math((A<long>)x, (A<long>)y, (a, b) => a > b ? a : b);
                }
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>)) {
                    return math((A<double>)x, (A<double>)y, (a, b) => a > b ? a : b);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => a > b ? a : b);
                }
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>)) {
                    return mathmixed((A<long>)x, (A<double>)y, (a, b) => a > b ? a : b);
                }
                else if (x.CanBeInt() && y.CanBeInt()) {
                    return mathmixedLong(x, y, (a, b) => a > b ? a : b);
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixed(x, y, (a, b) => a > b ? a : b);
                }
            }            
            else if (op == "+.") {
                if (x.TypeE == AType.Types.Bool && y.TypeE == AType.Types.Bool) {
                    return math(x as A<bool>, y as A<bool>, (a,b) => a || b);
                }
                if (x.TypeE == AType.Types.Long && y.TypeE == AType.Types.Long) {
                    return math(x as A<long>, y as A<long>, (a, b) => a | b);
                }
                if (x.TypeE == AType.Types.Decimal && y.TypeE == AType.Types.Decimal) {
                    return math(x as A<Decimal>, y as A<Decimal>, (a, b) => ((long)a) | ((long)b));
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixedLong(x, y, (a, b) => a | b);
                }
            }
            else if (op == "*.") {
                if (x.TypeE == AType.Types.Bool && y.TypeE == AType.Types.Bool) {
                    return math(x as A<bool>, y as A<bool>, (a, b) => a && b);
                }
                if (x.TypeE == AType.Types.Long && y.TypeE == AType.Types.Long) {
                    return math(x as A<long>, y as A<long>, (a, b) => a & b);
                }
                if (x.TypeE == AType.Types.Decimal && y.TypeE == AType.Types.Decimal) {
                    return math(x as A<Decimal>, y as A<Decimal>, (a, b) => ((long)a) & ((long)b));
                }
                else if (x.GetType() != y.GetType()) {
                    return mathmixedLong(x, y, (a, b) => a & b);
                }
            }

            else if (op == "%") {
                if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<decimal>)) {
                    return math((A<decimal>)x, (A<decimal>)y, (a, b) => b != 0 ? a / b : 0 );
                }
                else {
                    var a2 = x.ConvertDouble();
                    var b2 = y.ConvertDouble();
                    //round to 6 decimal points
                    //TODO: make option
                    return math(a2, b2, (a, b) => a == 0 && b == 0 ? 0 : Math.Round(a / b, 6));
                    //return math(a2, b2, (a, b) => a == 0 && b == 0 ? 0 : a / b);
                }
                
            }
            else if (op == "$") {
                if (x.GetType() == typeof(A<long>)) {
                    if (y.GetType() == typeof(A<long>)) {
                        return reshape((A<long>)x, (A<long>)y);
                    }
                    else if (y.GetType() == typeof(A<double>)) {
                        return reshape((A<long>)x, (A<double>)y);
                    }
                    else if (y.GetType() == typeof(A<JString>)) {
                        return reshape_str((A<long>)x, (A<JString>)y);
                    }
                    else {
                        return InvokeExpression("reshape", x, y, 1);
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
            else if (op == "~:") {
                if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<long>))
                    return notequals((A<long>)x, (A<long>)y);
                else if (x.GetType() == typeof(A<double>) && y.GetType() == typeof(A<double>))
                    return notequals((A<double>)x, (A<double>)y);
                else if (x.GetType() == typeof(A<long>) && y.GetType() == typeof(A<double>))
                    return notequals((A<long>)x, (A<double>)y);
                else if (x.GetType() == typeof(A<JString>) && y.GetType() == typeof(A<JString>))
                    return notequals((A<JString>)x, (A<JString>)y);
                else return InvokeExpression("notequals", x, y, 2);
            }
            else if (op == "#") {
                if (y.GetType() != typeof(A<JTable>)) {
                    return InvokeExpression("copy", x.ConvertLong(), y, 1);
                }
                else {
                    return copyTable(y as A<JTable>);
                }
                
            }
            else if (op == "i.") {
                return InvokeExpression("indexof", x, y, 1);
            }
            else if (op == ",") {
                if (x.GetType() == typeof(A<JTable>) && y.GetType() == typeof(A<JTable>)) {
                    return linktable((A<JTable>)x, (A<JTable>)y);
                }
                else if (x.GetType() == typeof(A<JTable>) && y.GetType() == typeof(A<Box>)) {
                    return linktableExpression((A<JTable>)x, (A<Box>)y);
                }
                //WARNING: invoke expression will convert to long
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<Decimal>)) {
                    return append(x as A<decimal>, y as A<decimal>);
                }
                return InvokeExpression("append", x, y, 1, tryConvertLong: false);
            }
            else if (op == "{.") {
                x = x.TryConvertLong(Conjunctions.Parser);
                if (y.GetType() == typeof(A<JTable>) && x.GetType() != typeof(A<long>)) {

                    A<JTable> yt = (A<JTable>)InvokeExpression("fromtable", x, y, 2);
                    var table = beheadTable(yt);
                    if (table.GetCount() == 1) {
                        return table.GetValA(0);
                    }
                    
                    var ytt = yt.First();
                    //handling for single row with multiple columns of all the same type
                    if (table.GetType() == typeof(A<Box>) && (ytt.RowCount <=1)) {
                        var allInt = ytt.Rows.Aggregate(true, (p,c) => p && c.val.GetType() == typeof(A<long>));
                        var allDouble = ytt.Rows.Aggregate(true, (p, c) => p && c.val.GetType() == typeof(A<double>));
                        var allDecimal = ytt.Rows.Aggregate(true, (p, c) => p && c.val.GetType() == typeof(A<decimal>));
                        if (allInt || allDouble || allDecimal) {
                            var ret = raze<Box>(table as A<Box>);
                            return ret;
                        }
                    }
                    
                    return table;
                }
                return InvokeExpression("take", x, y, 1);
            }
            else if (op == "}.") {
                return InvokeExpression("drop", x, y, 1);
            }
            else if (op == "{") {
                if (y.GetType() != typeof(A<JTable>)) {
                    if (x.GetType() != typeof(A<Boolean>)) {
                        return InvokeExpression("from", x, y, 1);
                    }
                    else if (x.GetType() == typeof(A<Boolean>)) {
                        //todo: shouldn't need to convert, but make from support bools natively
                        var newX = new A<long>(x.Shape);
                        newX.Ravel = (x as A<bool>).Ravel.Select(xv => (long)(xv ? 1 : 0)).ToArray();
                        return InvokeExpression("from", newX, y, 1);
                    }
                    

                } else {
                    return InvokeExpression("fromtable", x, y, 2);
                }
                
            }
            else if (op == "-:") {
                //temporary
                var z = new A<bool>(0);
                if (x.Type == typeof(Byte) && y.Type == typeof(Byte)) {
                    var ax = (A<Byte>)x;
                    var ay = (A<Byte>)y;

                    bool same = true;
                    for (var i = 0; i < ax.Count; i++) {
                        if (ax.Ravel[i] != ay.Ravel[i]) { same = false; break; }
                    }
                    z.Ravel[0] = same;
                        /*
                        var xv = System.Text.Encoding.UTF8.GetString(((A<Byte>)x).Ravel);
                        var yv = System.Text.Encoding.UTF8.GetString(((A<Byte>)y).Ravel);
                         */
                        //z.Ravel[0] = xv == yv;
                }
                else {
                    z.Ravel[0] = x.ToString() == y.ToString();
                }
                
                return z;
            }
            else if (op == "/:") {
                if (y.GetType() != typeof(A<JTable>)) {
                    return InvokeExpression("sortup", x, y, 2);
                }
                else {
                    return sortTable((A<JString>)x, (A<JTable>)y, "gradeup");
                }
                
            }
            else if (op == "\\:") {
                if (y.GetType() != typeof(A<JTable>)) {
                    return InvokeExpression("sortdown", x, y, 2);
                }
                else {
                    return sortTable((A<JString>)x, (A<JTable>)y, "gradedown");
                }                
            }
            else if (op == ";") {
                //WARNING: invoke expression will convert to long
                if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<Decimal>)) {
                    return link(x as A<decimal>, y as A<decimal>);
                }
                else if (x.GetType() == typeof(A<decimal>) && y.GetType() == typeof(A<Box>)) {
                    return link(x as A<decimal>, y as A<Box>);
                }    
                else 
                    return InvokeExpression("link", x, y, 2, tryConvertLong: false);                
            }
            else if (op == ",.") {
                return InvokeExpression("stitch", x, y, 1);                
            }
            else if (op == "I.") {
                return InvokeExpression("intervalIndex", x, y, 1, tryConvertLong: false);
            }
            else if (expressionMap.TryGetValue(op, out verbWithRank)) {
                if (verbWithRank.DyadicX == VerbWithRank.Infinite && verbWithRank.DyadicY == VerbWithRank.Infinite) {
                    return verbWithRank.DyadicFunc(x, y);
                }
                else if (verbWithRank.DyadicX == 0 && verbWithRank.DyadicY == 0) {
                    return verbWithRank.DyadicFunc(x, y);
                }
            }
            
            throw new NotImplementedException(op + " on x:" + x + " y:" + y + " type: " + y.GetType());
        }

        //crude code gen
        public string codeGenHotSpot(string[] lines) {
            StringBuilder sb = new StringBuilder();

            /* example
            using MicroJ;
            using System.Numerics;
            void func(MicroJ.Parser parser, System.Collections.Generic.Dictionary<string, AType> names) {
                names[""tmp""] = names[""x1""];
                names[""x1""] = names[""x2""];
                names[""x2""] = parser.Verbs.math((A<BigInteger>)names[""tmp""], (A<BigInteger>)names[""x1""], (a, b) => a + b);
            }
             */
         
            sb.AppendLine("using MicroJ;");
            sb.AppendLine("using System.Numerics;");
            sb.AppendLine("void func(MicroJ.Parser parser, System.Collections.Generic.Dictionary<string, AType> names) {");
            foreach (var line in lines) {
                var parts = line.Split(' ').Select(x=>x.Trim()).Where(x=>!String.IsNullOrEmpty(x)).ToArray();
                if (line.Contains("=.")) {
                    if (!line.Contains("+")) {
                        sb.AppendLine("names[\"" + parts[0] + "\"] = names[\"" + parts[2] + "\"];");
                    }
                    else if (line.Contains("+") && line.Contains("type:BigInteger")) {
                        sb.AppendLine("names[\"" + parts[0] + "\"] = parser.Verbs.math((A<BigInteger>)names[\"" + parts[2] + "\"], (A<BigInteger>) names[\"" + parts[4] + "\"],(a, b) => a + b);");
                    }
                }
            }
            sb.AppendLine("}");
            return sb.ToString();
         
        }

        public AType runExplicit(string def, AType y, AType x = null) {

            var oldLocals = Conjunctions.Parser.LocalNames;
            //ensure locals are restored after running an explicit (multiple depths of explicit)
            try {
                if (oldLocals != null) { Conjunctions.Parser.LocalNames = new Dictionary<string, AType>(oldLocals); }
                return _runExplicit(def, y, x);
            }
            finally {
                Conjunctions.Parser.LocalNames = oldLocals;
            }
        }
        private AType _runExplicit(string def, AType y, AType x) {

            
            var lines = def.Split('\n').SelectMany(xv => {
                var line = xv.Trim(new char[] { ' ', '\t' });
                if (line.StartsWith("if.")) {
                    //normalize def so if/elseif/else start on its own line to simplify parsing
                    line = line.Replace(" if.", "\nif.").Replace(" elseif.", "\nelseif.").Replace(" else.", "\nelse.");

                }
                return line.Split('\n');
            }).Where(xv => xv.Length > 0 && !xv.StartsWith("NB.")).ToArray();
            var parser = Conjunctions.Parser;

            if (parser.LocalNames == null) {
                parser.LocalNames = new Dictionary<string, AType>();
            }
            parser.LocalNames["y"] = y;
            if (x != null) {
                parser.LocalNames["x"] = x;
            }
            AType ret = null;
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
                    
                    parser.ReadLine = () => {
                        i++;
                        return lines[i];
                    };

                    if (line.StartsWith("if.")) {
                        var endIdx = -1;
                        for (var k = i; k < lines.Length; k++) {
                            if (lines[k].Contains("end.")) {
                                endIdx = k;
                                break;
                            }
                        }
                        if (endIdx == -1) { throw new ApplicationException("end not found"); }
                        AType t = null;
                        bool matched = false;
                        bool foundMatch = false;
                        bool done = false;
                        for (var k = i; k <= endIdx; k++) {
                            line = lines[k];
                            var origLine = line;
                            //dangling end. after else.
                            if (line.StartsWith("end.")) { continue; }
                            if ((line.StartsWith("if.") || line.StartsWith("elseif."))) {
                                matched = false;
                                if (!foundMatch) {                                    
                                    line = line.Replace("elseif.", "").Replace("if.", "").Replace("return.", "");
                                    var doIdx = line.IndexOf("do.");
                                    var test = line.Substring(0, doIdx);
                                    t = parser.parse(test);
                                    if (t.ToString() == "1") {
                                        var afterDo = doIdx + "do.".Length + 1;
                                        if (afterDo <= line.Length) {
                                            line = line.Substring(afterDo);
                                            ret = parser.parse(line);
                                            if (origLine.Contains("return.")) { return ret; }
                                        }                                        
                                        matched = true;
                                        foundMatch = true;
                                    }
                                }                                
                            }
                            else if (line.StartsWith("else.")) {
                                if (!foundMatch) {
                                    matched = true;
                                    line = line.Replace("else.", "").Replace("end.", "").Replace("return.", "");
                                    if (line.Length > 0) {
                                        ret = parser.parse(line);
                                    }                                    
                                    if (origLine.Contains("return.")) { return ret; }
                                }
                                else {
                                    done = true;
                                }                              
                            }
                            else {
                                if (matched && !done) {
                                    ret = parser.parse(line);
                                }                                
                            }
                        }
                        i = endIdx;
                    }
                    else if (line.StartsWith("while")) {
                        var test = line.Replace("do.", "").Replace("while.", "");
                        var endIdx = 0;
                        for (var k = (i + 1); k < lines.Length; k++) {
                            if (lines[k].StartsWith("end.")) {
                                endIdx = k;
                                break;
                            }
                        }
                        if (endIdx == 0) { throw new ApplicationException("end not found");  }

                        AType t = null;
                        while (true) {
                            t = parser.parse(test);
                            if (t.ToString() != "1") break;
                            for (var k = i + 1; k < endIdx; k++) {
                                ret = parser.parse(lines[k]);
                            }                            
                        }
                        i = endIdx;
                    }
                    else if (line.StartsWith("for")) {
                        var test = line.Substring(line.IndexOf(".") + 1).Replace("do.", "");
                        AType t = null;
                        t = parser.parse(test);
                        var endIdx = 0;
                        for (var k = (i + 1); k < lines.Length; k++) {
                            if (lines[k].StartsWith("end.")) {
                                endIdx = k;
                                break;
                            }
                        }
                        if (endIdx == 0) { throw new ApplicationException("end not found"); }

                        var rep = t.GetCount();
                        var names = parser.LocalNames;
                        if (line.Contains("!hotspot") && rep > 1000) {
                            var code = codeGenHotSpot(lines.Skip(i).Take(endIdx-1).ToArray());
                            var func = CSScript.LoadDelegate<Action<Parser, Dictionary<string, AType>>>(code, null, false, "System.Collections.Generic", "System.Numerics");
                            for (var n = 0; n < rep; n++) {
                                func(Conjunctions.Parser, names);    
                            }
                        }
                        else {
                            for (var n = 0; n < rep; n++) {
                                for (var k = i + 1; k < endIdx; k++) {
                                    ret = parser.parse(lines[k]);
                                }
                            }
                        }
                        
                        i = endIdx;
                    }
                    else {
                        ret = parser.parse(line);
                    }
                    
                    
                }
                catch (Exception e) {
                    //Console.WriteLine(line + "\n" + e);
                    if (Parser.ThrowError) { throw new ApplicationException(line + "\nwith: " + (y != null ? y.ToString() : "") + "\n" + (x!=null?x.ToString():"") + "\ndef: " + def, e); }
                }
            }
            return ret;
        }

        public AType unpivotTable(AType y) {
            var yt = (y as A<JTable>).First();
            var zt = new JTable {
                Columns = new string[] { yt.Columns[0], "value" }
            };
            var newCt = yt.RowCount * (yt.Columns.Length - 1);
            var keys = yt.Rows[0].val.Create(new long[] { newCt });
            var vals = yt.Rows[1].val.Create(new long[] { newCt });
            var keyRavel = yt.Rows[0].val;
            var rowIdx = 0;

            //TODO: potential optimization to avoid getVal/setVal
            for(var i = 0; i < yt.RowCount; i++) {
                var keyVal = keyRavel.GetVal(i);
                for (var k = 1; k < yt.Columns.Length; k++) {
                    keys.SetVal(rowIdx, keyVal);
                    vals.SetVal(rowIdx, yt.Rows[k].val.GetVal(i));
                    rowIdx++;
                }
                
            }
            zt.Rows = new Box[] { keys.Box(), vals.Box() };
            return zt.WrapA();
        }

        public AType catenate(A<JString> x, A<JString> y) {
            var max = Math.Max(x.Count, y.Count);
            var ret = new A<JString>(max);
            for (var i = 0; i < max; i++) {
                var str1 = i >= x.Ravel.Length ? x.GetString(0) : x.GetString(i);
                var str2 = i >= y.Ravel.Length ? y.GetString(0) : y.GetString(i);
                ret.Ravel[i] = new JString { str = str1 + str2 };
            }
            return ret;
        }
        //candidate for code generation
        public AType Call1(AType method, AType y) {
            var verbs = (A<Verb>)method;
            var verb = verbs.Ravel[0];

            var exp = verbs.ToString().Replace(" ", "");
            SpecialCodeEval eval;
            if (SpecialCode.TryGetValue(exp, out eval)) {
                if (eval.evalType(y)) {
                    return eval.monad(y);
                }
            }

            if (verb.explicitDef != null) {
                return runExplicit(verb.explicitDef, y);
            }

            //future: add check for integrated rank support
            if (verb.conj != null) {
                return Conjunctions.Call1(method, y);
            }
            else  if (verb.childVerb != null) {
                var v = verb.childVerb as A<Verb>;
                if (v != null) {
                    return Call1(v, y);
                }
            }
            else if (verbs.GetCount()  == 3) {
                AType z1 = null;
                //support noun in left tine
                Verb v = verbs.ToAtom(0).Ravel[0];
                if (v.rhs != null && v.op == null && v.childAdverb == null) {
                    z1 = AType.MakeA(v.rhs, null);
                }
                else {
                    z1 = Call1(verbs.ToAtom(0), y);
                }
                
                var z3 = Call1(verbs.ToAtom(2), y);
                var z2 = Call2(verbs.ToAtom(1), z1, z3);
                return z2;
            } 

            if (verb.adverb != null) { // || verb.childVerb   != null && ((Verb)verb.childVerb).adverb != null) {
                return Adverbs.Call1(method, y);
            }

            
            var op = verb.op;
            VerbWithRank verbWithRank = null;

            if (op == "]") {
                return y;
            }
            else if (op == "i.") {
                if (y.GetType() == typeof(A<int>)) {
                    return iota((A<int>)y);
                }
                else if (y.GetType() == typeof(A<long>)) {
                    return iota((A<long>)y);
                }
                else if (y.GetType() == typeof(A<decimal>)) {
                    return iota((A<decimal>)y);
                }
            }
            else if (op == "$") {
                return shape(y);
            }
            else if (op == "#") {
                return tally(y);
            }
            else if (op == "{.") {
                return InvokeExpression("head", y);
            }
            else if (op == "}.") {
                if (y.GetType() != typeof(A<JTable>)) {
                    return InvokeExpression("behead", y);
                }
                else {
                    return beheadTable((A<JTable>)y);
                }
                
            }
            else if (op == "}:") {
                return InvokeExpression("curtail", y);
            }
            else if (op == "<") {
                return box(y);
            }
            else if (op == ">") {
                if (y.GetType() == typeof(A<Box>)) {
                    return unbox((A<Box>)y);
                }
                else {
                    return y;
                }
                
            }
            else if (op == "|:") {
                if (y.GetType() == typeof(A<int>)) {
                    return transpose((A<int>)y);
                }
                else if (y.GetType() == typeof(A<long>)) {
                    return transpose((A<long>)y);
                }
            }
            else if (op == "|.") {
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
            else if (op == "<.") {
                return InvokeExpression("floor", y);
            }
            else if (op == ">.") {
                return InvokeExpression("ceiling", y);
            }
            else if (op == ";") {
                if (y.GetType() == typeof(A<Box>)) {
                    return raze<Box>((A<Box>)y);
                }
                else {
                    //raze seems to be like ravel for non-boxed
                    return InvokeExpression("ravel", y);
                }
            }
            else if (op == "/:") {
                return InvokeExpression("gradeup", y);
            }
            else if (op == "\\:") {
                return InvokeExpression("gradedown", y);
            }
            else if (op == "*:") {
                if (y.GetType() == typeof(A<long>)) {
                    return math((A<long>)y, (A<long>)y, (a, b) => a * b);
                }
                else if (y.GetType() == typeof(A<double>)) {
                    return math((A<double>)y, (A<double>)y, (a, b) => a * b);
                }                
            }
            else if (op == "+:") {
                if (y.GetType() == typeof(A<long>)) {
                    return math((A<long>)y, (A<long>)y, (a, b) => a + b);
                }
                else if (y.GetType() == typeof(A<double>)) {
                    return math((A<double>)y, (A<double>)y, (a, b) => a + b);
                }
            }
            else if (op == "!") {
                A<double> a = new A<double>(1);
                if (y is A<int> || y is A<long>)
                    a.Ravel[0] = Gamma.GammaReal(1.0 * (((A<long>)y).Ravel[0]));
                else a.Ravel[0] = Gamma.GammaReal(1.0 * (((A<double>)y).Ravel[0]));
                return a;
            }
            else if (op == "\":") {
                return InvokeExpression("tostring", y);
            }
            else if (op == "~.") {
                if (y.GetType() != typeof(A<JTable>)) {
                    return InvokeExpression("nub", y);
                }
                else {
                    return nubTable( (A<JTable>) y);
                }
                
            }
            else if (op == ",.") {
                return InvokeExpression("ravelitems", y);
            }
            else if (op == "|") {
                if (y.GetType() == typeof(A<long>)) {
                    return math(new A<long>(0), (A<long>)y, (a, b) => Math.Abs(b));
                }
                else if (y.GetType() == typeof(A<double>)) {
                    return math(new A<double>(0), (A<double>)y, (a, b) => Math.Abs(b));
                }
                else if (y.GetType() == typeof(A<decimal>)) {
                    return math(new A<decimal>(0), (A<decimal>)y, (a, b) => Math.Abs(b));
                }
                    
                
            }
            else if (expressionMap.TryGetValue(op, out verbWithRank)) {
                return verbWithRank.InvokeWithRank(y);
            }
            throw new NotImplementedException(op + " on y: " + y + " type: " + y.GetType());
        }
    }
    public class Conjunctions {
        //@ and ^: handled in special code only right now
        public static readonly string[] Words = new[] { "\"", "!:", "&", ":", "!.", "@", "^:" };
        public Verbs Verbs;
        public Dictionary<string, AType> Names;
        public Parser Parser;

        public Conjunctions(Verbs verbs) {
            Verbs = verbs;

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            AppDomain currentDomain = AppDomain.CurrentDomain;

            var binPath = Path.Combine(path, "bin");
            currentDomain.AssemblyResolve += new ResolveEventHandler((sender, args) => {
                var assemblyPath = Path.Combine(binPath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath) == false) return null;
                Assembly dependency = Assembly.LoadFrom(assemblyPath);
                return dependency;
            });
        }

        public AType rank1ex<T>(AType method, A<T> y)
            where T : struct {
            var verb = ((A<Verb>)method).Ravel[0];
            var newRank = Convert.ToInt32(verb.rhs);

            //create a new verb without the conj component so we can safely pass it around
            var newVerb = new A<Verb>(1);
            bool isTrain = verb.childVerb != null && verb.childVerb.GetType() == typeof(A<Verb>);
            if (isTrain) {
                newVerb.Ravel = (verb.childVerb as A<Verb>).Ravel;
            }
            else if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;
                newVerb.Ravel[0] = cv;
            }
            else {
                newVerb.Ravel[0] = new Verb { op = verb.op, adverb = verb.adverb };
            }

            if (newRank == y.Rank) { return Verbs.Call1(newVerb, y); }

            var shape = y.ShapeCopy();
            bool isString = y.GetType() == typeof(A<JString>);

            if (isString) {
                //drop the last part of the shape for strings, since it's an indirect reference
                shape = y.Shape.Take(y.Shape.Length - 1).ToArray();
            }
            var newShape = shape.Take(y.Rank - newRank).ToArray();


            var newCt = AType.ShapeProduct(newShape);
            var vs = new AType[newCt];
            var subShape = shape.Skip(y.Rank - newRank).ToArray();
            var subShapeCt = AType.ShapeProduct(subShape);
            var offset = 0;

            for (var i = 0; i < vs.Length; i++) {
                var newY = new A<T>(subShapeCt, subShape);
                for (var k = 0; k < newY.Count; k++) {
                    if (!isString || newRank > 0) {
                        newY.Ravel[k] = y.Ravel[offset];
                    }
                    else {
                        newY.Ravel[k] = (T)(object)y.GetCharJString(offset);
                    }
                    offset++;
                }
                vs[i] = Verbs.Call1(newVerb, newY);
            }
            var ct = vs.Length * vs[0].GetCount();

            if (vs[0].Shape != null) {
                newShape = newShape.Concat(vs[0].Shape).ToArray();
            }
       
            return vs[0].Merge(newShape, vs);          
        }
        
        public AType rank1table(AType method, A<JTable> y) {
            var yt = y.First();
            var vs = new AType[yt.RowCount];
            var verb = ((A<Verb>)method).Ravel[0];

            //create a new verb without the conj component so we can safely pass it around
            var newVerb = new A<Verb>(1);
            if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;
                newVerb.Ravel[0] = cv;
            }
            else {
                newVerb.Ravel[0] = new Verb { op = verb.op, adverb = verb.adverb, explicitDef = verb.explicitDef };
            }

            for(var i = 0; i < yt.RowCount; i++) {
                //subsetting the table for the verb is unstable currently, so we'll create a new table
                var rowIdx = yt.indices == null ? i : yt.indices[i];
                //var newY = yt.Clone();
                //newY.indices = new long[] { rowIdx };
                var newY = new JTable {
                    Columns = yt.Columns.Select(x=>x).ToArray()
                };
                var newRows = new Box[newY.Columns.Length];
                for (var k = 0; k < newY.Columns.Length;k++ ) {
                    newRows[k] = yt.Rows[k].val.FromIndices(new long[] { rowIdx }).Box();
                }
                newY.Rows = newRows;

                //todo move block to function
                var locals = new Dictionary<string, AType>();
                if (Parser.LocalNames != null) {
                    foreach (var kvx in Parser.LocalNames) {
                        locals[kvx.Key] = kvx.Value;
                    }
                }
                for (var ii = 0; ii < newY.Columns.Length; ii++) {
                    locals[JTable.SafeColumnName(newY.Columns[ii])] = newY.Rows[ii].val;
                }
                locals["this"] = newY.WrapA();

                var oldLocals = Parser.LocalNames;
                Parser.LocalNames = locals;
                vs[i] = Verbs.Call1(newVerb, newY.WrapA());
                Parser.LocalNames = oldLocals;
            }

            //dictionary
            if (vs[0].GetType() == typeof(A<Box>) && vs[0].Rank == 2 && vs[0].Shape[0] == 2) {
                vs = vs.Select(xv => JTable.FromDict(xv as A<Box>).WrapA()).ToArray();
            }
            //merge multiple tables back into 1
            if (vs[0].GetType() == typeof(A<JTable>)) {
                var vst = vs.Select(x=>(x as A<JTable>).First()).ToArray();
                var zt = new JTable {
                    Columns = vst[0].Columns.Select(x => x).ToArray()
                };
                
                var newCols = new Box[zt.Columns.Length];
                for (var i = 0; i < newCols.Length; i++) {
                    var newRows = new AType[vst.Length];
                    for(var k = 0; k < newRows.Length;k++) {
                        if (vst[k].indices != null && vst[k].indices.Length == 0) {
                            newRows[k] = vst[k].Rows[i].val.FromIndices(new long[] { -1 });
                            //throw new ApplicationException("Cannot merge an empty row");
                        }
                        else {
                            newRows[k] = vst[k].Rows[i].val.GetValA(vst[k].indices != null ? vst[k].indices[0] : 0);
                        }
                        
                    }

                    var newShape = new long[] { yt.RowCount };
                    if (vs[0].Shape != null) {
                        newShape = newShape.Concat(vs[0].Shape).ToArray();
                    }
                    newCols[i] = newRows[0].Merge(newShape, newRows).Box();
                }
                zt.Rows = newCols;
                return zt.WrapA();
            }
            
            else {
                var newShape = new long[] { yt.RowCount };
                if (vs[0].Shape != null) {
                    newShape = newShape.Concat(vs[0].Shape).ToArray();
                }
                else {
                    if (vs[0].GetType() == typeof(A<JString>)) {
                        newShape = new long[] { yt.RowCount,1 };
                    }
                }
                return vs[0].Merge(newShape, vs);
            }
            
        }

        public AType matchfast<T>(AType method, A<T> x, A<T> y) where T : struct {
            var z = new A<bool>(y.Shape[0]);
            var ax = (A<Byte>)(object)x;
            var ay = (A<Byte>)(object)y;
            var xv = System.Text.Encoding.UTF8.GetString(ax.Ravel);
            var ct = (int) y.Shape[y.Shape.Length - 1];
            for (int i = 0; i < y.Shape[0]; i++ ) {
                var yv = System.Text.Encoding.UTF8.GetString(ay.Ravel, (i * ct), ct);
                z.Ravel[i] = xv.Equals(yv);
            }

            return z;
        }

        public AType takesubstringfast(A<long> x, A<JString> y) {
            long[] newShape = null;

            var xct = Math.Abs(x.Ravel[0]);
            newShape = new long[] { y.Shape[0], xct };
            var v = new A<JString>(newShape);

            for (var i = 0; i < y.Shape[0]; i++) {
                v.Ravel[i] = new JString { str = String.Intern(y.Ravel[i].str.Substring(0, (int)x.Ravel[0])) };
            }
            return v;
        }


        public AType rank2ex<T, T2>(AType method, A<T> x, A<T2> y)
            where T : struct where T2 : struct {
            var verb = ((A<Verb>)method).Ravel[0];
            var newRank = Convert.ToInt32(verb.rhs);

            //create a new verb without the conj component so we can safely pass it around
            var newVerb = new A<Verb>(1);
            if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;
                newVerb.Ravel[0] = cv;
            }
            else {
                newVerb.Ravel[0] = new Verb { op = verb.op, adverb = verb.adverb };
            }

            if (newRank == y.Rank) { return Verbs.Call2(newVerb, x, y); }

            var shape = y.ShapeCopy();
            bool isString = y.GetType() == typeof(A<JString>);

            if (isString) {
                //drop the last part of the shape for strings, since it's an indirect reference
                shape = y.Shape.Take(y.Shape.Length - 1).ToArray();
            }
            var newShape = shape.Take(y.Rank - newRank).ToArray();


            var newCt = AType.ShapeProduct(newShape);
            var vs = new AType[newCt];
            var subShape = shape.Skip(y.Rank - newRank).ToArray();
            var subShapeCt = AType.ShapeProduct(subShape);
            var offset = 0;

            for (var i = 0; i < vs.Length; i++) {
                var newY = new A<T2>(subShapeCt, subShape);
                for (var k = 0; k < newY.Count; k++) {
                    if (!isString || newRank > 0) {
                        newY.Ravel[k] = y.Ravel[offset];
                    }
                    else {
                        newY.Ravel[k] = (T2)(object)y.GetCharJString(offset);
                    }
                    offset++;
                }
                vs[i] = Verbs.Call2(newVerb, x, newY);
            }
            var ct = vs.Length * vs[0].GetCount();

            if (vs[0].Shape != null) {
                newShape = newShape.Concat(vs[0].Shape).ToArray();
            }

            return vs[0].Merge(newShape, vs);
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
        Dictionary<string, Func<AType, Parser, AType>> dotnetMethodCache = null;
        public AType calldotnet<T>(A<T> x, A<JString> y) where T : struct {

            

#if CSSCRIPT
            if (dotnetMethodCache == null) { dotnetMethodCache = new Dictionary<string, Func<AType, Parser, AType>>(); }
            Func<AType, Parser, AType> func = null;
            if (!dotnetMethodCache.TryGetValue(y.Ravel[0].str, out func)) {
                if (Parser.SafeMode) { throw new AccessViolationException(); }
                var code = y.Ravel[0].str;
                var lines = code.Split('\n');
                var usings = String.Join("\n", lines.Where(t => t.StartsWith("//css_using ")).Select(t => "using " + t.Replace("//css_using ", "") + ";").ToArray());
                var refs = lines.Where(t => t.StartsWith("//css_ref ")).SelectMany(t => t.Replace("//css_ref ", "").Split(',')).Select(t => t.Trim()).ToArray();
                var codecs = "using MicroJ;\n" + usings + "\n" + "MicroJ.AType func (MicroJ.AType v, MicroJ.Parser parser) { " + code + " }";
                func = CSScript.LoadDelegate<Func<AType, Parser, AType>>(codecs, null, false, refs);
                dotnetMethodCache[y.Ravel[0].str] = func;
            }
            var ret = func(x, Parser);
            return ret;
#else
            var v = new A<JString>(0);
            v.Ravel[0] = new JString { str = "microj must be compiled with csscript support" };
            return v;
#endif

        }

        //BYTE=1, JCHAR = 2, JFL = 8
        //(2;12) (151!:0) 'dates';'c:/temp/dates.bin';
        //(<8) (151!:0) 'tv';'c:/temp/tv.bin';                
        public unsafe AType readmmap(A<Box> x, A<Box> y, Verb verb, long[] offsets = null) {
            string name = ((A<JString>)y.Ravel[0].val).Ravel[0].str;
            string file = ((A<JString>)y.Ravel[1].val).Ravel[0].str;            
            long type = ((A<long>)x.Ravel[0].val).Ravel[0];
            long size = 0;
            if (x.Count > 1) {
                size = ((A<long>)x.Ravel[1].val).Ravel[0];
            }

            
            var num = new FileInfo(file).Length;
            using (var mmf = MemoryMappedFile.CreateFromFile(file, FileMode.Open)) {
                using (var view = mmf.CreateViewAccessor(0, num)) {                                        
                    byte* ptr = (byte*)0;
                    AType val = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);                                                
                    if (type == 1) {
                        var rows = size > 0 ? num / size : num;
                        byte[] arr = new byte[num];
                        System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)num);                        
                        val = new A<Byte>(new long[] { rows, size }) { Ravel = arr };
                    }
                    else if (type == 2) {
                        var rows = size > 0 ? num / size : num;
                        var vals = new JString[rows];
                        for (var i = 0; i < rows; i++) {
                            byte[] arr = new byte[size];
                            System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), (int)(i*size)), arr, 0, (int)size);                        
                            vals[i] = new JString { str = String.Intern(System.Text.Encoding.UTF8.GetString(arr)) };
                        }                        
                        val = new A<JString>(new long[] { rows, size }) { Ravel = vals };
                    }                  
                    else if (type == 8) {
                        var rows = num / sizeof(double);
                        double[] arr = new double[rows];
                        System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)rows);
                        val = new A<double>(new long[] { rows }) { Ravel = arr };
                    }
                    else if (type == 4) {
                        var rows = num / sizeof(long);
                        
                        int offset = 0;
                        if (offsets != null) {
                            offset = (int)offsets[0] * sizeof(long);                            
                            rows = (offsets[1] - offsets[0]);
                            if (offsets[1] == -1) {
                                rows = (int)(num - offset) / sizeof(long);
                            }
                        }
                        long[] arr = new long[rows];
                        System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), offset), arr, 0, (int)rows);
                        val = new A<long>(new long[] { rows }) { Ravel = arr };
                    }
                    view.SafeMemoryMappedViewHandle.ReleasePointer();
                    Parser.Names[name] = val;
                    return val;
                    
                }
                
            }
            throw new ApplicationException("unknown type");
        }

        private Tuple<Stopwatch, List<double>> measureTime(Tuple<Stopwatch, List<double>> measurement) {
#if DEBUG
            Stopwatch watch;
            List<double> times;
            if (measurement == null) {
                watch = new Stopwatch();
                times = new List<double>();
            }
            else {
                watch = measurement.Item1;
                times = measurement.Item2;
            }
            if (!watch.IsRunning) { 
                watch.Start();  
            }
            else {
                watch.Stop();
                times.Add(watch.ElapsedMilliseconds);
                watch.Start();
            }
            return new Tuple<Stopwatch, List<double>>(watch, times);
#else 
            return null;
#endif
        }
        //basic csv reading implementation. see readcsv in stdlib.ijs for a more robust example
        public AType readcsv(A<Box> xb, A<Box> y) {

            int TYPE_INT = 3, TYPE_DOUBLE = 2, TYPE_STR = 1;

            var stopWatch = measureTime(null);

            string fileName = ((A<JString>)y.Ravel[0].val).Ravel[0].str;
            string tableName = ((A<JString>)y.Ravel[1].val).Ravel[0].str;
            var optionsDict = new Dictionary<string, string>();
            if (y.Ravel.Length >= 3) {
                optionsDict = AHelper.ToOptions((A<Box>)y.Ravel[2].val);
            }
            if (xb != null) {
                optionsDict = AHelper.ToOptions(xb);
            }
            bool keepLeadingZero = optionsDict.ContainsKey("KeepLeadingZero");

            var newNames = new List<string>();

            var limit = optionsDict.ContainsKey("limit") ? Int64.Parse(optionsDict["limit"]) : Int64.MaxValue;
            var delimiter = !optionsDict.ContainsKey("delimiter") ? ',' : Char.Parse(optionsDict["delimiter"].Replace("\\t", "\t"));
            var unquote = optionsDict.ContainsKey("unquote");
            HashSet<string> keepColumns = null;
            if (optionsDict.ContainsKey("cols")) {
                keepColumns = new HashSet<string>();
                keepColumns.UnionWith(optionsDict["cols"].Split(',').Select(x=>x.Trim()));
            }

            

            bool noPad = optionsDict.ContainsKey("nopad");
            bool noUTF8 = optionsDict.ContainsKey("noutf8");

            var iLine = 0;
            var types = new int[500];
            string[] headers = null;
            int fieldCount = 0;
            //using (var csv = new CsvReader(new StreamReader(fileName), true, delimiter)) {

            stopWatch = measureTime(stopWatch);

            var headerLine = File.ReadLines(fileName).First();
            headers = headerLine.Split(delimiter).Select(x=>x.Trim()).ToArray();
            if (unquote) {
                headers = headers.Select(x => x.Trim('\"')).ToArray();
            }
            fieldCount = headers.Length;

            int[] manualTypes = null;
            if (optionsDict.ContainsKey("types")) {
                manualTypes = optionsDict["types"].Select(x => {
                    if (x == 'I') {
                        return TYPE_INT;
                    }
                    else if (x == 'F') {
                        return TYPE_DOUBLE;
                    }
                    else {
                        return TYPE_STR;
                    }
                }).ToArray();
            }

            stopWatch = measureTime(stopWatch);

            int warningCt = 0;
            if (manualTypes == null) {
                foreach (var line in File.ReadLines(fileName).Skip(1)) {
                    var csv = line.Split(delimiter);
                    if (unquote) {
                        csv = csv.Select(x => x.Trim('\"')).ToArray();
                    }
                    if (iLine++ > 1000 && iLine > limit) { break; }
                    for (var k = 0; k < fieldCount; k++) {
                        int n = 0;
                        double d = 0;              
                        if (csv.Length < fieldCount) {
                            if (warningCt < 5) 
                                Console.WriteLine("WARNING: Line is less than field count" + line);
                            warningCt++;
                            continue;
                        }
                        if (csv[k] == "") continue;
                        if ((types[k] == 0 || types[k] == 3) && Int32.TryParse(csv[k], out n)) {
                            if (csv[k].StartsWith("0") && keepLeadingZero && csv[k].Length > 1) {
                                types[k] = TYPE_STR;
                            }
                            else {
                                types[k] = TYPE_INT;
                            }
                        }
                        else if ((types[k] == 0 || types[k] == TYPE_DOUBLE || types[k] == TYPE_INT) && Double.TryParse(csv[k], out d)) {
                            types[k] = TYPE_DOUBLE;
                        }
                        else {
#if DEBUG
                            if (types[k] != TYPE_STR && types[k] != 0) {
                                Debug.WriteLine("Promoting " + headers[k] + " to string " + csv[k]);
                            }
#endif
                            types[k] = TYPE_STR;
                        }
                    }
                }
            }
            else {
                types = manualTypes;
            }
            
            stopWatch = measureTime(stopWatch);
            /*
            for (int i = 0; i < fieldCount; i++) {
                Console.WriteLine(string.Format("{0} = {1}; Type={2}",
                                                headers[i], csv[i], types[i]));
            }
            */

            

            var finalColumnNames = new List<string>();
            var boxedRows = new List<Box>();

            var strings = new Dictionary<string, List<string>>();
            var longs = new Dictionary<string, List<long>>();
            var decimals = new Dictionary<string, List<decimal>>();
            var doubles = new Dictionary<string, List<double>>();
            var rowCount = 0;

            foreach (var line in File.ReadLines(fileName).Skip(1)) {
                
                var csv = line.Split(delimiter);
                if (unquote) {
                    csv = csv.Select(x => x.Trim('\"')).ToArray();
                }
                if (noUTF8) {
                    csv = csv.Select(x => Regex.Replace(x, @"[^\u0000-\u007F]", string.Empty)).ToArray();
                }
                if (rowCount >= limit) { break; }

                rowCount++;
                var keptColumnCt = 0;
                for (int i = 0; i < fieldCount; i++) {
                    var columnName = headers[i];
                    if (keepColumns != null && !keepColumns.Contains(columnName)) { continue; }

                    var columnType = types[keptColumnCt];
                    

                    
                    keptColumnCt++;

                    if (Parser.UseDecimal && (columnType == TYPE_INT || columnType == TYPE_DOUBLE)) {
                        if (!decimals.ContainsKey(columnName)) {
                            decimals[columnName] = new List<decimal>();
                        }
                        decimal dv;
                        if (!Decimal.TryParse(csv[i] != "" ? csv[i] : "0", out dv)) {
                            Console.WriteLine("bad data: " + csv[i]);
                        }
                        decimals[columnName].Add(dv);
                    }
                    else if (columnType == TYPE_INT) {
                        if (!longs.ContainsKey(columnName)) {
                            longs[columnName] = new List<long>();
                        }
                        long lv;
                        if (!Int64.TryParse(csv[i] != "" ? csv[i] : "0", out lv)) {
                            Console.WriteLine("bad data: " + csv[i]);
                        }
                        longs[columnName].Add(lv);
                    }
                    else if (columnType == TYPE_STR) {
                        if (!strings.ContainsKey(columnName)) {
                            strings[columnName] = new List<string>();
                        }
                        var sv = i < csv.Length ? csv[i] : "";
                        strings[columnName].Add(sv);
                    }
                    else if (columnType == TYPE_DOUBLE) {
                        if (!doubles.ContainsKey(columnName)) {
                            doubles[columnName] = new List<double>();
                        }
                        var val = csv[i] != "" ? csv[i] : "0";
                        if (manualTypes != null) {
                            val = val.Replace(",", "").Replace("$", "").Replace("\"","").Trim();
                        }
                        double dv;
                        Double.TryParse(val, out dv);
                        doubles[columnName].Add(dv);

                    }
                    /*
                        else if (columnType == "System.DateTime") {
                        if (!strings.ContainsKey(columnName)) {
                        strings[columnName] = new List<string>();
                        }
                        DateTime dv = (DateTime) (reader.IsDBNull(i) ? DateTime.MinValue : reader.GetDateTime(i));
                        strings[columnName].Add(dv.ToString("yyyy-MM-dd"));
                        }
                    */
                }
            }

            stopWatch = measureTime(stopWatch);
            Func<string, string> createName = s => {
                return JTable.SafeColumnName(s) + "_" + tableName + "_";
            };

            if (keepColumns == null) {
                keepColumns = new HashSet<string>();
                keepColumns.UnionWith(headers);
            }
            foreach (var col in keepColumns) {
                AType val;
                if (decimals.ContainsKey(col)) {
                    val = new MicroJ.A<decimal>(rowCount) { Ravel = decimals[col].ToArray() };
                }
                else if (longs.ContainsKey(col)) {
                    val = new MicroJ.A<long>(rowCount) { Ravel = longs[col].ToArray() };
                }
                else if (doubles.ContainsKey(col)) {
                    val = new MicroJ.A<double>(rowCount) { Ravel = doubles[col].ToArray() };
                }
                else if (strings.ContainsKey(col)) {
                    var max = strings[col].Select(x => x.Length).Max();
                    val = new MicroJ.A<JString>(rowCount, new long[] { rowCount, max }) { Ravel = strings[col].Select(x => new MicroJ.JString { str = String.Intern( !noPad ? x.PadRight(max) : x) }).ToArray() };
                } else {
                    throw new ApplicationException("col: " + col + " not found");
                }
                var newName = createName(col);
                newNames.Add(newName);
                Parser.Names[newName] = val;
                finalColumnNames.Add(col);
                boxedRows.Add(val.Box());
            }

            stopWatch = measureTime(stopWatch);
            //return new MicroJ.A<MicroJ.JString>(new long[] { newNames.Count, 100 }) { Ravel = newNames.Select(x=>new MicroJ.JString { str = x }).ToArray() };
            var ret = new JTable {
                Columns = finalColumnNames.ToArray(),
                Rows = boxedRows.ToArray()
            }.WrapA();

            stopWatch = measureTime(stopWatch);
            return ret;
        }
        public AType runfile(AType y, Verb verb) {

            string[] lines;

            if (y.GetType() == typeof(A<Box>)) {
                if (Parser.SafeMode) { throw new AccessViolationException(); }
            
                string file = ((A<JString>)(y as A<Box>).Ravel[0].val).Ravel[0].str;

                if (!File.Exists(file)) {
                    Console.WriteLine("file: " + file + " does not exist");
                }
                lines = File.ReadAllLines(file);
            }
            else if (y.GetType() == typeof(A<JString>)) {
                lines = (y as A<JString>).GetVal(0).ToString().Replace("\r", "").Split('\n').ToArray();
            }
            else {
                throw new NotImplementedException("runfile not implemented on: " + y.GetType());
            }
            
            var oldRL = Parser.ReadLine;

            AType lastEval = null;
            for (var i = 0; i < lines.Length; i++) {
                var line = lines[i];
                try {
                    if (line.StartsWith("NB.") || line.TrimStart(new char[] { ' ', '\t' }).Length == 0) continue;
                    if (line.StartsWith("exit")) break;
                    if (line.StartsWith("!B")) {
                        Debugger.Launch();
                        Debugger.Break();
                        line = line.Substring(2, line.Length - 2);
                    }
                    Parser.ReadLine = () => {
                        i++;
                        return lines[i];
                    };
                    if (verb.rhs == "1") {
                        Console.WriteLine(line);
                    }
                    var ret = Parser.parse(line);
                    lastEval = ret;
                }
                finally { }
            }
            Parser.ReadLine = oldRL;
            var z = new A<JString>(0);
            if (verb.rhs == "1") {                
                z.Ravel[0].str = lastEval.ToString();
            }
            if (verb.rhs == "4") {
                return lastEval;
            }

            return z;
        }

        public AType timeit(A<JString> y) {
            var z = new A<double>(0);
            var watch = new Stopwatch();
            watch.Start();
            Parser.exec(y.Ravel[0].str);
            watch.Stop();
            z.Ravel[0] = watch.Elapsed.TotalSeconds;
            return z;
        }


        public AType tableToJSON(A<JTable> y) {
            var yt = y.First();

            var rows = new List<Dictionary<string, object>>();
            for (var i = 0; i < yt.RowCount; i++) {
                var idx = yt.indices != null ? yt.indices[i] : i;
                var row = new Dictionary<string, object>();
                for(var k = 0 ; k < yt.Columns.Length; k++) {
                    var val = yt.Rows[k].val.GetVal(idx);
                    if (val.GetType() == typeof(JString)) {
                        row[yt.Columns[k]] = val.ToString();
                    }
                    else {
                        row[yt.Columns[k]] = val;
                    }
                    
                }
                rows.Add(row);
            }
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;

            return new JString { str =serializer.Serialize(rows) }.WrapA();
        }

        public AType tableFromJSON(A<JString> y) {
            var str = y.First();
            var serializer = new JavaScriptSerializer();
            var obj = serializer.Deserialize<List<Dictionary<string, object>>>(str.str);

            var columns = new List<string>();
            var first = obj.First();
            foreach (var key in first.Keys) {
                columns.Add(key);
            }
            var rowCount = obj.Count;
            var rows = new Box[columns.Count];
            for (var i = 0; i < columns.Count; i++) {
                var key = columns[i];
                var val = first[key];

                var allInt = obj.Select(x => x[key].GetType() == typeof(int)).Where(x => x).Count() == obj.Count();

                if (Parser.UseDecimal && (val.GetType() == typeof(long) || val.GetType() == typeof(int) || val.GetType() == typeof(double) || val.GetType() == typeof(decimal))) {
                    var vals = obj.Select(x => Convert.ToDecimal(x[key])).ToArray();
                    rows[i] = new A<decimal>(rowCount) { Ravel = vals }.Box();
                }
                else if (val.GetType() == typeof(long)) {
                    var vals = obj.Select(x => (long)x[key]).ToArray();
                    rows[i] = new A<long>(rowCount) { Ravel = vals }.Box();
                }
                else if (val.GetType() == typeof(int) && allInt) {
                    var vals = obj.Select(x => (long)(int)x[key]).ToArray();
                    rows[i] = new A<long>(rowCount) { Ravel = vals }.Box();
                }
                else if (val.GetType() == typeof(double) || val.GetType() == typeof(decimal) || val.GetType() == typeof(int)) {
                    var vals = obj.Select(x => Convert.ToDouble(x[key])).ToArray();
                    rows[i] = new A<double>(rowCount) { Ravel = vals }.Box();
                }
                else if (val.GetType() == typeof(string)) {
                    var vals = obj.Select(x => new JString { str=String.Intern(x[key].ToString()) }).ToArray();
                    rows[i] = new A<JString>(rowCount) { Ravel = vals }.Box();
                }
            }
            return new JTable {
                Columns = columns.ToArray(),
                Rows = rows
            }.WrapA();
                
        }

        public AType writeTableKeys(A<Box> x, A<JTable> y) {
            var yt = y.First();
            var path = x.First().val.GetString(0);
            var keyName = x.Ravel[1].val.GetString(0);

            if (yt.Key == null || yt.Key.Keys.Count() == 0) {  throw new ApplicationException("keys cannot be null or empty"); }

            var maxKey = yt.Key.Keys.Select(xv => xv.Length).Max();

            using (var fs = new FileStream(path + "\\" + keyName + "_s" + maxKey + ".key", FileMode.Create)) {
                using (var bw = new BinaryWriter(fs)) {
                    foreach (var key in yt.Key.Keys) {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(key.PadRight(maxKey));                                
                        bw.Write(bytes); 
                    }
                }
            }

            long pos = 0L;
            using (var fs = new FileStream(path + "\\" + keyName + "_l-keyoffset.key", FileMode.Create)) {
                using (var bw = new BinaryWriter(fs)) {
                    foreach (var key in yt.Key.Keys) {                        
                        bw.Write(pos);
                        pos += yt.Key[key].Count;
                    }
                }
            }
            
            using (var fs = new FileStream(path + "\\" + keyName + "_l.key", FileMode.Create)) {
                using (var bw = new BinaryWriter(fs)) {
                    foreach (var val in yt.Key.Values) {
                        foreach (var idx in val) {
                            bw.Write(idx);
                        }
                    }
                }
            }
            return y;
        }

        public unsafe AType readTableKey(A<Box> x, A<Box> y) {
            var path = x.First().val.GetString(0);
            var keyName = x.Ravel[1].val.GetString(0);

            var files = new DirectoryInfo(path).GetFiles(keyName + "_s*" + ".key");
            var keyFile = files.FirstOrDefault();
            if (keyFile == null) { throw new ApplicationException("key " + keyName + " not found in " + path);  }
            var parts = keyFile.FullName.Split('_');
            var len = Convert.ToInt32(parts[parts.Length - 1].Substring(1).Replace(".key", ""));

            var keys = readmmap(Box.BoxLongs(2, 2), 
                new A<Box>(2) { Ravel = new Box[] { 
                    new JString { str = keyName + "_key" }.WrapA().Box(), 
                    new JString { str = keyFile.FullName }.WrapA().Box() } 
                }, new Verb()) as A<JString>;

            var keyOffset = readmmap(Box.BoxLongs(4),
                new A<Box>(2) {
                    Ravel = new Box[] { 
                    new JString { str = keyName + "_keyoffset" }.WrapA().Box(), 
                    new JString { str = Path.Combine(path, keyName + "_l-keyoffset.key") }.WrapA().Box() }
                }, new Verb()) as A<long>;

            
            var foundIdx = -1;
            var searchVal = y.Ravel[0].val.GetString(0);
            for (var i = 0; i < keys.Ravel.Length; i++) {
                if (keys.Ravel[i].str == searchVal) { foundIdx = i; break; }
            }

            long[] offsets = new long[2];
            if (foundIdx < keys.Ravel.Length-1) {
                offsets[0] = keyOffset.Ravel[foundIdx];
                offsets[1] = keyOffset.Ravel[foundIdx+1];
            }
            else {
                offsets[0] = keyOffset.Ravel[foundIdx];
                offsets[1] = -1;
            }

            var keyIdx = readmmap(Box.BoxLongs(4),
                new A<Box>(2) {
                    Ravel = new Box[] { 
                    new JString { str = keyName + "_keys" }.WrapA().Box(), 
                    new JString { str = Path.Combine(path, keyName + "_l.key") }.WrapA().Box() }
                }, new Verb(), offsets:offsets) as A<long>;

            var newRowLen = keyIdx.Ravel.Length;

            files = new DirectoryInfo(path).GetFiles("*.bin");

            string[] cols = new string[files.Length];
            Box[] newRows = new Box[files.Length];

            int offset = 0;
            foreach (var file in files) {
                parts = file.Name.Split('_');
                var spec = parts[0];
                var col = String.Join("", parts.Skip(1).ToArray()).Replace(".bin", "");
                var num = file.Length;

                using (var mmf = MemoryMappedFile.CreateFromFile(path + "\\" + file.Name, FileMode.Open)) {
                    AType val = null;
                    using (var view = mmf.CreateViewStream(0, num)) {
                        using(var bw = new BinaryReader(view)) {
                            
                            if (spec.StartsWith("b")) {
                                /*
                                int size = Int32.Parse(spec.Substring(1));
                                var rows = size > 0 ? num / size : num;
                                byte[] arr = new byte[num];
                                System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)num);
                                val = new A<Byte>(new long[] { rows, size }) { Ravel = arr };
                                 */
                            }
                            else if (spec.StartsWith("s")) {
                                var arr = new JString[newRowLen];
                                int size = Int32.Parse(spec.Substring(1));
                                for (var i = 0; i < keyIdx.Ravel.Length; i++) {
                                    var idx = keyIdx.Ravel[i];
                                    view.Seek(idx * size, SeekOrigin.Begin);
                                    byte[] bytes = bw.ReadBytes(size);
                                    arr[i] =  new JString { str = String.Intern(System.Text.Encoding.UTF8.GetString(bytes)) };
                                }
                                val = new A<JString>(new long[] { newRowLen, size }) { Ravel = arr };
                                /*
                                int size = Int32.Parse(spec.Substring(1));
                                var rows = size > 0 ? num / size : num;
                                var vals = new JString[rows];
                                for (var i = 0; i < rows; i++) {
                                    byte[] arr = new byte[size];
                                    System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), (int)(i * size)), arr, 0, (int)size);
                                    vals[i] = new JString { str = String.Intern(System.Text.Encoding.UTF8.GetString(arr)) };
                                }
                                val = new A<JString>(new long[] { rows, size }) { Ravel = vals };
                                 */
                            }
                            else if (spec.StartsWith("d")) {
                                /*
                                var rows = num / sizeof(double);
                                double[] arr = new double[rows];
                                System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)rows);
                                val = new A<double>(new long[] { rows }) { Ravel = arr };
                                 */
                            }
                            else if (spec.StartsWith("l")) {
                                long[] arr = new long[newRowLen];
                                for (var i = 0; i < keyIdx.Ravel.Length; i++) {
                                    var idx = keyIdx.Ravel[i];
                                    view.Seek(idx * sizeof(long), SeekOrigin.Begin);
                                    arr[i] = bw.ReadInt64();
                                }
                                val = new A<long>(new long[] { newRowLen }) { Ravel = arr };
                            }
                        }
                        cols[offset] = col;
                        newRows[offset] = val.Box();
                        offset++;
                    }
                }         
            }
            return new JTable {
                Columns = cols,
                Rows = newRows
            }.WrapA();
            
        }

        public AType writeTableCSV(A<Box> x, A<JTable> y) {
            var path = x.First().val.GetString(0);

            var optionsDict = new Dictionary<string, string>();
            if (x.Count > 1) {
                //writeBytes = true;
                optionsDict = AHelper.ToOptions((x.Ravel[1].val) as A<Box>);
            }
            var yt = y.First();
            var delim = "\t";
            using (var sw = new StreamWriter(path)) {
                sw.WriteLine(String.Join(delim, yt.Columns));
                var colCt = yt.Columns.Length;

                for(var i = 0; i < yt.RowCount; i++) {
                    var idx = yt.indices != null ? yt.indices[i] : i;
                    var row = new string[colCt];
                    for(var k = 0; k < colCt; k++ ) {
                        row[k] = yt.Rows[k].val.GetString(idx);
                    }                        
                    sw.WriteLine(String.Join(delim, row));
                }
            }

            return AType.MakeA(new FileInfo(path).Length);
        }
        public AType writeTableBinary(A<Box> x, A<JTable> y) {
            var yt = y.First();
            var path = x.First().val.GetString(0);
            bool writeBytes = false;
            var optionsDict = new Dictionary<string, string>();
            if (x.Count > 1) {
                //writeBytes = true;
                optionsDict = AHelper.ToOptions((x.Ravel[1].val) as A<Box>);
            }
            if (optionsDict.ContainsKey("type") && optionsDict["type"] == "csv") {
                return writeTableCSV(x, y);
            }
            for (var k = 0; k < yt.Columns.Length; k++) {
                var col = JTable.SafeColumnName(yt.Columns[k]);
                var prefix = "";
                int maxLen = 0;
                if (yt.Rows[k].val.GetType() == typeof(A<long>)) {
                    prefix = "l_";
                }
                else if (yt.Rows[k].val.GetType() == typeof(A<double>)) {
                    prefix = "d_";
                }
                else if (yt.Rows[k].val.GetType() == typeof(A<JString>)) {
                    maxLen = (yt.Rows[k].val as A<JString>).Ravel.Select(xx => xx.str.Length).Max();
                    if (!writeBytes) {                        
                        prefix = "s" + maxLen.ToString() + "_";
                    }
                    else {
                        prefix = "b" + maxLen.ToString() + "_";
                    }
                    
                }
                using (var fs = new FileStream(path + "\\" + prefix + col + ".bin", FileMode.Create)) {
                    using (var bw = new BinaryWriter(fs)) {
                        for(var i = 0; i < yt.RowCount; i++) {
                            var idx = yt.indices != null ? yt.indices[i] : i;
                            var val = yt.Rows[k].val.GetVal(idx);
                            if (yt.Rows[k].val.GetType() == typeof(A<long>)) {
                                bw.Write((long)val);
                            }
                            else if (yt.Rows[k].val.GetType() == typeof(A<decimal>)) {
                                bw.Write((decimal)val);
                            }
                            else if (yt.Rows[k].val.GetType() == typeof(A<double>)) {
                                bw.Write((double)val);
                            }
                            else if (yt.Rows[k].val.GetType() == typeof(A<JString>)) {
                                var str = val.ToString().PadRight(maxLen);
                                var bytes = System.Text.Encoding.UTF8.GetBytes(str);                                
                                bw.Write(bytes);
                            }                            
                        }
                    }
                }
            }
            return new JString { str = "" }.WrapA();
        }

        public unsafe AType readTableBinary(A<Box> x, A<Box> y) {
            var path = y.First().val.GetString(0);

            var files = new DirectoryInfo(path).GetFiles("*.bin");

            string[] cols = new string[files.Length];
            Box[] newRows = new Box[files.Length];

            var optionsDict = new Dictionary<string, string>();
            
            if (x != null) {
                optionsDict = AHelper.ToOptions(x);
            }
            bool noPad = optionsDict.ContainsKey("nopad");

            int offset = 0;
            foreach (var file in files) {
                var parts = file.Name.Split('_');
                var spec = parts[0];
                var col = String.Join("_", parts.Skip(1).ToArray()).Replace(".bin", "");
                var num = file.Length;

                using (var mmf = MemoryMappedFile.CreateFromFile(path + "\\" + file.Name, FileMode.Open)) {
                    using (var view = mmf.CreateViewAccessor(0, num)) {
                        byte* ptr = (byte*)0;
                        AType val = null;
                        view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        if (spec.StartsWith("b")) {
                            int size = Int32.Parse(spec.Substring(1));
                            var rows = size > 0 ? num / size : num;
                            byte[] arr = new byte[num];
                            System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)num);
                            val = new A<Byte>(new long[] { rows, size }) { Ravel = arr };
                        }
                        else if (spec.StartsWith("s")) {
                            int size = Int32.Parse(spec.Substring(1));
                            var rows = size > 0 ? num / size : num;
                            var vals = new JString[rows];
                            for (var i = 0; i < rows; i++) {
                                byte[] arr = new byte[size];
                                System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), (int)(i * size)), arr, 0, (int)size);
                                var str = System.Text.Encoding.UTF8.GetString(arr);
                                if (noPad) { str = str.TrimEnd();  }
                                vals[i] = new JString { str = String.Intern(str) };
                            }
                            val = new A<JString>(new long[] { rows, size }) { Ravel = vals };
                        }
                        else if (spec.StartsWith("d")) {
                            var rows = num / sizeof(double);
                            double[] arr = new double[rows];
                            System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)rows);
                            if (!Parser.UseDecimal) {
                                val = new A<double>(new long[] { rows }) { Ravel = arr };
                            }
                            else {
                                val = new A<decimal>(new long[] { rows }) { Ravel = arr.Select(xv=>Convert.ToDecimal(xv)).ToArray() };
                            }
                            
                        }
                        else if (spec.StartsWith("l")) {
                            var rows = num / sizeof(long);
                            long[] arr = new long[rows];
                            System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(new IntPtr(ptr), 0), arr, 0, (int)rows);
                            if (!Parser.UseDecimal) {
                                val = new A<long>(new long[] { rows }) { Ravel = arr };
                            }
                            else {
                                val = new A<decimal>(new long[] { rows }) { Ravel = arr.Select(xv => Convert.ToDecimal(xv)).ToArray() };
                            }
                            
                        }
                        view.SafeMemoryMappedViewHandle.ReleasePointer();
                        cols[offset] = col;
                        newRows[offset] = val.Box();
                        offset++;
                    }
                }
            }
               

            return new JTable {
                Columns = cols,
                Rows = newRows
            }.WrapA();
        }

        public AType Call1(AType method, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];

            //rank
            if (verb.conj == "\"") {
                //future: add special code for +/"n or use some type of integrated rank support

                //not sure if this is the right way to deal with boxes yet
                bool isTrain = verb.childVerb.GetType() == typeof(A<Verb>);
                if (y.GetType() == typeof(A<Box>) || (!isTrain && verb.childVerb != null && ((Verb)verb.childVerb).op == "<")) {
                    if (y.GetType() == typeof(A<long>)) {
                        return rank1ex<long>(method, (A<long>)y);
                    }
                    else if (y.GetType() == typeof(A<JString>)) {
                        return rank1ex<JString>(method, (A<JString>)y);
                    }
                    else if (y.GetType() == typeof(A<double>)) {
                        return rank1ex<double>(method, (A<double>)y);
                    }
                    else if (y.GetType() == typeof(A<bool>)) {
                        return rank1ex<bool>(method, (A<bool>)y);
                    }
                    else if (y.GetType() == typeof(A<Box>)) {
                        return rank1ex<Box>(method, (A<Box>)y);
                    }
                    else if (y.GetType() == typeof(A<Byte>)) {
                        return rank1ex<Byte>(method, (A<Byte>)y);
                    }

                }
                if (y.GetType() == typeof(A<JTable>)) {
                    return rank1table(method, (A<JTable>)y);
                }
                if (y.GetType() == typeof(A<long>)) { return rank1ex<long>(method, (A<long>)y); }
                //todo: evaluate performance of dynamic dispatch of rank -- probably ok
                else return Verbs.InvokeExpression("rank1ex", method, y, 1, this);
            }
            //bond
            else if (verb.conj == "&") {
                AType x;
                var newVerb = new A<Verb>(0);

                x = AType.MakeA(verb.rhs, Names);

                if (verb.childVerb != null) {
                    AType z;
                    if (x.GetType() == typeof(A<Verb>)) {
                        //(*: & *:) 4
                        z = Verbs.Call1(x, y);

                        if (verb.childVerb != null) {
                            newVerb.Ravel[0] = (Verb)verb.childVerb;
                            z = Verbs.Call1(newVerb, z);
                        }
                    }
                    else {
                        //plus=: +&2
                        newVerb.Ravel[0] = (Verb)verb.childVerb;
                        if (x.GetType() == typeof(A<Undefined>)) {
                            return z = Verbs.Call1(newVerb, y);
                        }
                        else {
                            return z = Verbs.Call2(newVerb, y, x);
                        }
                        
                    }
                    
                    
                    return z;
                }
                else {
                    //plusx=: 2&+
                    x = AType.MakeA(verb.op, Names);
                    newVerb.Ravel[0].op = verb.rhs;
                    return Verbs.Call2(newVerb, x, y);
                }
            }
            else if (verb.conj == ":" && verb.op == "0") {
                if (verb.rhs == "0") {
                    var v = new A<JString>(0);
                    v.Ravel[0] = new JString { str = y.ToString() };
                    return v;
                }
            }
            else if (verb.conj == ":" && verb.op == "3") {
                if (verb.rhs == "0") {
                    var v = new A<Verb>(0);
                    v.Ravel[0] = new Verb { explicitDef = y.ToString() };
                    return v;
                }
                else if (verb.rhs.StartsWith("'")) { 
                    return Verbs.runExplicit(verb.rhs.TrimStart('\'').TrimEnd('\''), y);
                }
                else {                    
                    return Verbs.runExplicit(AType.MakeA(verb.rhs, Names).ToString(), y);
                }
            }
            else if (verb.conj == ":" && verb.op == "4") {
                if (verb.rhs == "0") {
                    var v = new A<Verb>(0);
                    v.Ravel[0] = new Verb { explicitDef = y.ToString() };
                    return v;
                }
            }
            else if (verb.conj == "!:" && verb.op == "0") {
                return runfile(y,verb);
            }
            else if (verb.conj == "!:" && verb.op == "9" && verb.rhs == "100") {
                Parser.UseDecimal = Convert.ToInt32(y.GetString(0)) == 1 ? true : false;
                if (Parser.UseDecimal) {
                    Parser.Names["UseDecimal"] = AType.MakeA(1);
                }
                else {
                    Parser.Names.Remove("UseDecimal");
                }
                return y;
            }
            else if (verb.conj == "!:" && verb.op == "4" && verb.rhs == "1") {
                //names       
                var type = (y as A<long>).First();
                var vals = Parser.Names.Where(xv => {
                    var isVerb = xv.Value.GetType() == typeof(A<Verb>);
                    if (type == 0) {
                        return !isVerb;
                    }
                    else {
                        return isVerb;
                    }
                }).Select(xv => new JString { str = xv.Key }.WrapA()).Select(xv => xv.Box()).ToArray();
                if (vals.Length > 0) {
                    var z = new A<Box>(vals.Length);
                    z.Ravel = vals;
                    return z;
                }
                else {
                    return new JString { str = "" }.WrapA();
                }

            }
            else if (verb.conj == "!:" && verb.op == "4" && verb.rhs == "0") {
                return Verbs.expressionMap["nameClass"].InvokeWithRank(y);
            }
            else if (verb.conj == "!:" && verb.op == "4" && verb.rhs == "100") {
                return new JString { str = y.GetType().ToString() }.WrapA();
            }
            else if (verb.conj == "!:" && verb.op == "3" && verb.rhs == "100") {
                var str = y.ToString();
                var z = new A<Byte>(str.Length);
                z.Ravel = System.Text.UTF8Encoding.UTF8.GetBytes(str);
                return z;
            }
            else if (verb.conj == "!:" && verb.op == "13" && verb.rhs == "100") {
                throw new ApplicationException(y.ToString());
            }

            else if (verb.conj == "!:" && verb.op == "3" && verb.rhs == "101") {
                if (y.GetType() == typeof(A<long>)) {
                    return new A<BigInteger>(1) { Ravel = new BigInteger[] { new BigInteger(y.GetLong(0)) } };
                }
                else if (y.GetType() == typeof(A<JString>)) {
                    return new A<BigInteger>(1) { Ravel = new BigInteger[] { BigInteger.Parse(y.GetString(0)) } };
                }
            }
            else if (verb.conj == "!:" && verb.op == "3" && verb.rhs == "102") {
                if (y.GetType() == typeof(A<Box>)) {
                    var yb = y as A<Box>;
                    var table = new A<JTable>(1);
                    table.Ravel[0] = new JTable {
                        Columns = (yb.Ravel[0].val as A<Box>).Ravel.Select(x => ((A<JString>)x.val).Ravel[0].str).ToArray(),
                        Rows = yb.Ravel.Skip(1).ToArray()
                    };

                    return table;
                }
                else if (y.GetType() == typeof(A<JString>)) {
                    return tableFromJSON((A<JString>)y);
                }
            }
            else if (verb.conj == "!:" && verb.op == "3" && verb.rhs == "103") {
                if (y.GetType() == typeof(A<JTable>)) {
                    return tableToJSON((A<JTable>)y);
                }
            }
            else if (verb.conj == "!:" && verb.op == "6" && verb.rhs == "2") {
                return timeit((A<JString>)y);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "1") {
                return readcsv(null, (A<Box>)y);
            }            
            throw new NotImplementedException(verb + " on y:" + y + " type: " + y.GetType());
        }

        public AType Call2(AType method, AType x, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            if (verb.conj == "!:" && verb.op == "150") {
                if (x.GetType() == typeof(A<JString>)) {
                    return calldotnet((A<JString>)x, (A<JString>)y);
                }
                else if (x.GetType() == typeof(A<long>)) {
                    return calldotnet((A<long>)x, (A<JString>)y);
                }
                else if (x.GetType() == typeof(A<double>)) {
                    return calldotnet((A<double>)x, (A<JString>)y);
                }
                else if (x.GetType() == typeof(A<Box>)) {
                    return calldotnet((A<Box>)x, (A<JString>)y);
                }
            }
            else if (verb.conj == "\"") {
                if (verb.childVerb != null && ((Verb)verb.childVerb).op == "-:" && x.Type == typeof(Byte) && y.Type == typeof(Byte)) {
                    return Verbs.InvokeExpression("matchfast", x, y, 1, this, method);
                }
                if (verb.childVerb != null && ((Verb)verb.childVerb).op == "{." && x.Type == typeof(long) && y.Type == typeof(JString)) {
                    return takesubstringfast((A<long>)x, (A<JString>)y);
                }
                return Verbs.InvokeExpression("rank2ex", x, y, 2, this,method);
            }
            else if (verb.conj == ":" && verb.op == "4") {
                if (verb.rhs == "0") {
                    var v = new A<Verb>(0);
                    v.Ravel[0] = new Verb { explicitDef = y.ToString() };
                    return v;
                }
                else {
                    return Verbs.runExplicit(verb.rhs.TrimStart('\'').TrimEnd('\''), y, x);
                }
            }            
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "0") {
                return readmmap((A<Box>)x, (A<Box>)y, verb);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "1") {
                return readcsv((A<Box>)x, (A<Box>)y);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "2") {
                return writeTableBinary((A<Box>)x, (A<JTable>)y);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "3") {
                return readTableBinary((A<Box>)x, (A<Box>)y);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "4") {
                return writeTableKeys((A<Box>)x, (A<JTable>)y);
            }
            else if (verb.conj == "!:" && verb.op == "151" && verb.rhs == "5") {
                return readTableKey((A<Box>)x, (A<Box>)y);
            }
            //default value for missing colum
            else if (verb.conj == "!." && y.GetType() == typeof(A<JTable>) && (verb.childVerb != null && verb.childVerb.ToString() == "{")) {
                //todo move somewhere else?
                var yt = (y as A<JTable>).First();
                var xb = (x as A<Box>);
                var col = xb.First().val.GetString(0);
                if (!yt.Columns.Contains(col)) {
                    return new JTable {
                        Columns = new string[] { col  },
                        Rows = new Box[] {
                            AType.MakeA(verb.rhs, Names).Box()
                        }
                    }.WrapA();
                }
                else {
                    return Verbs.fromtable(x as A<Box>, y as A<JTable>);                
                }
                
            }
            //default value for missing colum
            else if (verb.conj == "!." && y.GetType() == typeof(A<JTable>) && (verb.childVerb != null && verb.childVerb.ToString() == "{.")) {
                //todo move somewhere else?
                var yt = (y as A<JTable>).First();
                var xb = (x as A<Box>);
                var col = xb.First().val.GetString(0);
                if (!yt.Columns.Contains(col)) {
                    return AType.MakeA(verb.rhs, Names);
                }
                else {
                    return Verbs.Call2(AType.MakeA("{.", null), x, y);
                }

            }
            else if (verb.conj == "!." && y.GetType() == typeof(A<JTable>) && (verb.childVerb != null && verb.childVerb.ToString().Trim()== "/.")) {
                A<Verb> newVerb = new A<Verb>(0) { Ravel = new Verb[] { (Verb)verb.childVerb } };
                newVerb.Ravel[0].rhs = verb.rhs;
                return Verbs.Adverbs.keyTable( newVerb, x as A<Box>, y as A<JTable>);           
            }
            else if (verb.conj == "!." && y.GetType() == typeof(A<JTable>) && (verb.childVerb != null && verb.childVerb.ToString().Trim() == "}")) {
                A<Verb> newVerb = new A<Verb>(0) { Ravel = new Verb[] { (Verb)verb.childVerb } };
                newVerb.Ravel[0].rhs = verb.rhs;
                return Verbs.Adverbs.amendTable((AType)newVerb.First().childNoun, x as A<JTable>, y as A<JTable>, true);
            }
            else if (verb.conj == "&") {
                AType xv;
                var newVerb = new A<Verb>(0);

                xv = AType.MakeA(verb.rhs, Names);
                newVerb.Ravel[0] = (Verb)verb.childVerb;
                var z = Verbs.Call1(x, xv);
                return z;
            }
            else if (verb.conj == "@") {
                if (verb.rhs == "[" && verb.childVerb != null) {
                    A<Verb> newVerb = new A<Verb>(0) { Ravel = new Verb[] { (Verb)verb.childVerb } };
                    return Verbs.Call1(newVerb, x);
                }
            }
            throw new NotImplementedException(verb + " on y:" + y + " type: " + y.GetType());
        }

    }
    public class Adverbs {
        public static readonly string[] Words = new[] { "/", "/.", "~", "}", "\\" };
        public Verbs Verbs;
        public Conjunctions Conjunctions;

        public Adverbs(Verbs verbs) {
            Verbs = verbs;
        }

        //todo move to utility
        public static long prod(long[] ri) {
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

        //special code for +/ rank 1 (decimal)
        public A<decimal> reduceplus(A<decimal> y) {
            var v = new A<decimal>(0);
            decimal total = 0;
            for (var i = 0; i < y.Count; i++) {
                total += (decimal)y.Ravel[i];
            }
            v.Ravel[0] = total;
            return v;
        }

        //special code for +/\ rank 1 (decimal)
        public A<decimal> runsum(A<decimal> y) {
            var v = new A<decimal>(y.Count);
            decimal total = 0;
            for (var i = 0; i < y.Count; i++) {
                v.Ravel[i] = total += (decimal)y.Ravel[i];
            }            
            return v;
        }

        //special code for +/\ rank 1 (decimal)
        public A<long> runsum(A<long> y) {
            var v = new A<long>(y.Count);
            long total = 0;
            for (var i = 0; i < y.Count; i++) {
                v.Ravel[i] = total += (long)y.Ravel[i];
            }
            return v;
        }
        //special code for +/\ rank 1 (decimal)
        public A<double> runsum(A<double> y) {
            var v = new A<double>(y.Count);
            double total = 0;
            for (var i = 0; i < y.Count; i++) {
                v.Ravel[i] = total += (double)y.Ravel[i];
            }
            return v;
        }
        public A<T> reduceboxed<T>(AType op, A<Box> y) where T : struct {
            if (y.Rank == 1) {
                var v = new A<T>(0);
                v = (A<T>)Verbs.Call2(op, y.ToAtom(y.Count - 2).Ravel[0].val, y.ToAtom(y.Count - 1).Ravel[0].val);
                for (var i = y.Count - 3; i >= 0; i--) {
                    v = (A<T>)Verbs.Call2(op, y.ToAtom(i).Ravel[0].val, v);
                }
                return v;
            }
            throw new NotImplementedException();
        }

        public A<T> reduce<T>(AType op, A<T> y) where T : struct {
            if (y.Rank == 0 || y.Count == 1) {
                return y;
            }
            if (y.Rank == 1) {
                var v = new A<T>(0);
                v = (A<T>)Verbs.Call2(op, y.ToAtom(y.Count - 2), y.ToAtom(y.Count - 1));
                for (var i = y.Count - 3; i >= 0; i--) {
                    v = (A<T>)Verbs.Call2(op, y.ToAtom(i), v);
                }
                return v;
            }
            else {
                var indices = Verbs.permutationIdx(y.Shape);
                var subShape = y.Shape.Skip(1).ToArray();
                int subShapeCt = (int) prod(subShape);
                
                //this reduce is not from the left?
                var xt = y.FromIndices(indices.Take(subShapeCt).ToArray());
                xt.Shape = subShape;
                var yt = y.FromIndices(indices.Skip(subShapeCt).Take(subShapeCt).ToArray());
                yt.Shape = subShape;
                var v = (A<T>)Verbs.Call2(op, xt, yt);

                var taken = subShapeCt * 2;
                for (int i = (int)y.Shape[0] - 3; i >= 0; i--) {
                    var subIndices = new long[subShapeCt];
                    for (var k = 0; k < subShapeCt; k++) {
                        subIndices[k] = indices[k+taken];
                    }
                    taken += subShapeCt;
                    yt = y.FromIndices(subIndices);
                    yt.Shape = subShape;
                    v = (A<T>)Verbs.Call2(op, v, yt);
                    
                }
                return v;
            }
            throw new NotImplementedException();
        }

        public AType reflex<T>(AType op, A<T> x, A<T> y) where T : struct {
            var v = Verbs.Call2(op, x, y);
            return v;
        }

        //where options=noparse, the operation is not parsed
        public AType keyTable<T2, T>(AType op, A<T2> x, A<T> y) where T : struct where T2 : struct {
            var yt = (y as A<JTable>).First();
            var rowCt = yt.RowCount;

            var keyIndices = new Dictionary<string, List<long>>();

            int[] colIdx = null;
            //specify columns to key by
            if (x != null) {
                colIdx = (x as A<Box>).Ravel.Select(xv => Array.IndexOf(yt.Columns, xv.val.ToString())).ToArray();
                for (var i = 0; i < rowCt; i++) {
                    var rowIdx = yt.indices == null ? i : yt.indices[i];
                    string key = "";
                    for (var k = 0; k < colIdx.Length; k++) {
                        key = key + yt.Rows[colIdx[k]].val.GetString(rowIdx);
                    }
                    List<long> match = null;
                    if (!keyIndices.TryGetValue(key, out match)) {
                        match = new List<long>();
                        keyIndices[key] = match;
                    }
                    match.Add(rowIdx);
                }
            }
            else {
                //no columns to key by, but need to respect indices in case its filtered

                //todo likely performance issue
                //hack to support insert: '# Item_UOM' / ((<'Item_UOM') { input)
                var indices = new List<long>();
                for (var i = 0; i < rowCt; i++) {
                    var rowIdx = yt.indices == null ? i : yt.indices[i];
                    indices.Add(rowIdx);
                }
                keyIndices[""] = indices;
                
            }
            
            var keyCt = keyIndices.Count;

            var vop = op as A<Verb>;

            //key itself using footer expressions or #
            if (op.ToString() == "]") {
                if (colIdx == null) { throw new DomainException();  }
                var colCt = yt.Columns.Length;

                var rows = new Box[colCt];

                var t = new JTable {
                    Columns = yt.Columns,
                    Rows = rows,
                };

                

                for (var k = 0; k < colCt; k++) {
                    if (colIdx.Contains(k)) {
                        var vals = keyIndices.Keys.Select(xv => new JString { str = yt.Rows[k].val.GetString(keyIndices[xv].First()) }).ToArray();
                        var maxLen = vals.Select(xv => xv.str.Length).Max();
                        rows[k] = new Box {
                            val = new A<JString>(new long[] { keyCt, maxLen }) { Ravel = vals }
                        };
                    }
                    else {
                        if (yt.FooterExpressions != null && yt.FooterExpressions.ContainsKey(yt.Columns[k])) {
                            var parser = new Parser { UseDecimal = Conjunctions.Parser.UseDecimal };
                            parser.Names = Conjunctions.Parser.Names;

                            var rowCtA = new A<long>(0) { Ravel = new long[] { rowCt } };

                            var groupRows = new List<AType>();
                            var groupCt = 0;
                            foreach (var kv in keyIndices) {
                                var locals = new Dictionary<string, AType>();

                                if (Conjunctions.Parser.LocalNames != null) {
                                    foreach (var kvx in Conjunctions.Parser.LocalNames) {
                                        locals[kvx.Key] = kvx.Value;
                                    }
                                }
                                

                                locals["_N"] = rowCtA;
                                locals["_I"] = new A<long>(0) { Ravel = new long[] { groupCt++ } };
                                locals["_G"] = new A<long>(0) { Ravel = new long[] { kv.Value.Count } }; 
                                for (var i = 0; i < yt.Columns.Length; i++) {
                                    locals[JTable.SafeColumnName(yt.Columns[i])] = yt.Rows[i].val.FromIndices(kv.Value.ToArray());
                                }
                                var expressionResult = parser.exec(yt.FooterExpressions[yt.Columns[k]], locals);
                                groupRows.Add(expressionResult);
                            }
                            var at = groupRows[0].Merge(new long[] { keyCt }, groupRows.ToArray());
                            rows[k] = new Box {
                                val = at
                            };
                        }
                        else {
                            var cts = keyIndices.Select(xv => (long)xv.Value.Count()).ToArray();
                            rows[k] = new Box {
                                val = new A<long>(new long[] { keyCt }) { Ravel = cts }
                            };
                        }
                    }
                }
                return t.WrapA();
            }
            else if (vop != null && vop.Ravel[0].childNoun != null) {
                
                var noun = vop.Ravel[0].childNoun as A<Box>;

                string[] expressions;
                long colCt;
                //multiple expressions
                if (noun != null) {
                    colCt = noun.Count + ((colIdx != null) ? colIdx.Length : 0);
                    expressions = noun.Ravel.Select(xv => xv.val.ToString()).ToArray();
                }
                else {
                    colCt = 1;
                    expressions = new string[] { (vop.Ravel[0].childNoun as A<JString>).GetString(0) };
                }
                var rows = new Box[colCt];

                //clean up col names - smarter defaults
                var expressionCols = new List<string>();
                for(var i = 0; i < expressions.Length; i++) {
                    string col = expressions[i];
                    var parts = col.Split(' ');
                    //eg: is {. MSA
                    if (parts[0] == "is") {
                        col = parts.Last();
                        expressions[i] = String.Join(" ", parts.Skip(1));
                    }
                    //eg MSA is {. MSA
                    else if (parts.Length > 1 && parts[1] == "is") {
                        col = parts[0];
                        expressions[i] = String.Join(" ", parts.Skip(2));
                    }
                    expressionCols.Add(col);
                }

                var cols = colIdx != null ? yt.Columns.Where((xv, i) => colIdx.Contains(i)).ToArray().Concat(expressionCols).ToArray() : expressionCols.ToArray();
                
                var t = new JTable {
                    Columns = cols,
                    Rows = rows,
                };
                var colOffset = 0;

                //special fit to not parse expressions /. !. 'noparse'
                bool noParse = vop.Ravel[0].rhs != null && vop.Ravel[0].rhs.Contains("noparse");

                for (var k = 0; colIdx != null && k < colIdx.Length; k++) {
                    var groupRows = new List<AType>();

                    foreach (var kv in keyIndices) {
                        groupRows.Add((AType)yt.Rows[colIdx[k]].val.FromIndices(new long[] { kv.Value.First() }));
                    }

                    var newShape = new long[] { keyCt };
                    if (groupRows[0].GetType() == typeof(A<JString>)) {
                        //todo: dumb for now
                        newShape = new long[] { keyCt, 1 };
                    }
                    var at = groupRows[0].Merge(newShape, groupRows.ToArray());
                    rows[colOffset++] = new Box {
                        val = at
                    };                  
                }

                


                for(var k = 0; k < expressions.Length;k++) {
                    var parser = new Parser { UseDecimal = Conjunctions.Parser.UseDecimal };
                    parser.Names = Conjunctions.Parser.Names;
                    var expression = expressions[k];
                    var expressionParts = expression.Split(' ');
                    var groupRows = new List<AType>();

                    foreach (var kv in keyIndices) {

                        var rowCtA = new A<long>(0) { Ravel = new long[] { rowCt } };

                        var locals = new Dictionary<string, AType>();

                        if (Conjunctions.Parser.LocalNames != null) {
                            foreach (var kvx in Conjunctions.Parser.LocalNames) {
                                locals[kvx.Key] = kvx.Value;
                            }
                        }

                        long groupCt = 0;
                        locals["_N"] = rowCtA;
                        locals["_G"] = new A<long>(0) { Ravel = new long[] { kv.Value.Count } };
                        locals["_I"] = new A<long>(0) { Ravel = new long[] { groupCt++ } };

                        

                        AType expressionResult = null;
                        for (var i = 0; i < yt.Columns.Length; i++) {
                            locals[JTable.SafeColumnName(yt.Columns[i])] = yt.Rows[i].val.FromIndices(kv.Value.ToArray());
                        }
                        
                        
                        if (noParse) {
                            var method = new A<Verb>(1) { Ravel = new Verb[] { new Verb { op = expressionParts[0] } } };
                            expressionResult = Verbs.Call1(method, locals[expressionParts[1]]);
                        }
                        else if (expression.StartsWith("!")) {
                            var method = new A<Verb>(1) { Ravel = new Verb[] { new Verb { op = expressionParts[1]} } };
                            expressionResult = Verbs.Call1(method, locals[expressionParts[2]]);
                        }
                        else {
                            expressionResult = parser.exec(expression, locals);
                        }
                        //var expressionResult = new A<long>(0);
                        groupRows.Add(expressionResult);
                    }

                    var newShape = new long[] { keyCt };
                    if (groupRows[0].GetType() == typeof(A<JString>)) {
                        //todo: dumb for now
                        newShape = new long[] { keyCt, 1 };
                    }

                    //todo: hack to support /
                    if (op.ToString().Trim() != "/") {
                        var at = groupRows[0].Merge(newShape, groupRows.ToArray());
                        rows[colOffset++] = new Box {
                            val = at
                        };
                    }
                    else {
                        rows[colOffset++] = groupRows[0].Box();
                    }
                    
                }
                return t.WrapA();
            }
            else {
                throw new DomainException();
            }
            //var indices = Verbs.NubIndex<T2>(x);
        }
        public AType key<T2, T>(AType op, A<T2> x, A<T> y) where T : struct where T2 : struct {
            var indices = Verbs.NubIndex<T2>(x);

            long offset = 0;
            var vs = new AType[indices.Count()];            
            foreach (var index in indices.Values) {              
                var indexArray =index.ToArray();
                var vals = Verbs.from<T>(new A<long>(indexArray.Length) { Ravel = indexArray }, y);
                vs[offset++] = Verbs.Call1(op, vals);                
            }

            long[] newShape = new long[] { vs.Length };
            var ct = vs.Length * vs[0].GetCount();
            if (vs[0].Shape != null) {
                newShape = newShape.Concat(vs[0].Shape).ToArray();
            }
            return vs[0].Merge(newShape, vs);

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

        public A<JTable> setTableProps(AType noun, A<JTable> y) {
            if (noun.GetType() != typeof(A<Box>)) {
                throw new DomainException();
            }
            var yt = y.First();
            var options = AHelper.ToOptions(noun as A<Box>);
            if (options.ContainsKey("ukey")) {
                yt.UniqueKeys = new Dictionary<string, long>();
                var colIdx = yt.GetColIndex(new JString { str = options["ukey"] }.WrapA());
                for (var i = 0; i < yt.RowCount; i++) {
                    var rowIdx = yt.indices == null ? i : yt.indices[i];
                    yt.UniqueKeys[yt.Rows[colIdx].val.GetString(rowIdx).ToUpper()] = rowIdx;
                }
            }
            else if (options.ContainsKey("key")) {
                yt.Key = new Dictionary<string, List<long>>();
                var colIdx = yt.GetColIndex(new JString { str = options["key"] }.WrapA());
                for (var i = 0; i < yt.RowCount; i++) {
                    var rowIdx = yt.indices == null ? i : yt.indices[i];
                    List<long> indices = null;
                    var key = yt.Rows[colIdx].val.GetString(rowIdx).ToUpper();
                    if (!yt.Key.TryGetValue(key, out indices)) {
                        indices = new List<long>();
                        yt.Key[key] = indices;
                    }
                    indices.Add(rowIdx);
                }
            }
            //replace instance copied by runtime since jtable is a struct
            y.Ravel[0] = yt;
            return y;
        }

        public A<JTable> lookupApprox<T>(JTable foreignTable, int foreignKeyCol, JTable primaryTable, int primaryKeyCol, bool noOrig = false, string lookupCol = null) where T : struct, IComparable {

            T[] joinVals = (foreignTable.Rows[foreignKeyCol].val as A<T>).Ravel;
            var newRows = new Box[foreignTable.Columns.Length];
            var newCols = foreignTable.Columns.Select(x => x).ToArray();

            
            for (var k = 0; k < newCols.Length; k++) {
                var indices = new long[primaryTable.RowCount];
                for (var i = 0; i < primaryTable.RowCount; i++) {                    
                    T searchVal = (T)primaryTable.Rows[primaryKeyCol].val.GetVal(i);
                    long idx = Array.BinarySearch<T>(joinVals, searchVal);
                    if (idx < 0) {
                        idx = ~idx;
                    }

                    if (idx < joinVals.Length) {
                        indices[i] = idx;
                    }
                    else {
                        indices[i] = -1;
                    }                    
                }
                newRows[k] = foreignTable.Rows[k].val.FromIndices(indices).Box();
            }

            var newT = new JTable { Columns = newCols, Rows = newRows };
            if (!noOrig && lookupCol == null) {
                primaryTable = Verbs.linktable(primaryTable.WrapA(), newT.WrapA()).First();
            }
            else {
                return newT.WrapA();
            }

            return primaryTable.WrapA();

        }
        public A<JTable> amendTable(AType noun, AType newVal, A<JTable> y, bool approx = false) {
            var yt = y.First();

            var yl = noun as A<long>;
            if (yl != null && yl.Ravel[0] == -1) {
                //yt.FooterExpressions = noun;
                var xb = (newVal as A<Box>);
                yt.FooterExpressions = new Dictionary<string, string>();
                var frameCt = xb.Rank > 1 ? xb.Shape[xb.Shape.Length - 1] : 1;
                var frameOffset = xb.Rank > 1 ? frameCt : 1;
                for (var i = 0; i < frameCt; i++) {
                    yt.FooterExpressions[xb.Ravel[i].val.ToString()] = xb.Ravel[(i + frameOffset)].val.ToString();
                }
                return yt.WrapA();
            }
            else if (noun.GetType() != typeof(A<Box>)) {
                throw new DomainException();
            }
            var yb = (noun as A<Box>);
            var idx = Array.IndexOf(yt.Columns, yb.Ravel[0].val.ToString());
            
            var keyIdx = 0;

            if (newVal.GetType() != typeof(A<JTable>)) {
                if (yb.Count == 3) {
                    keyIdx = yt.GetColIndex(yb.Ravel[2].val);
                }
            }
            else {
                keyIdx = yt.GetColIndex(yb.Ravel[0].val);
            }

            var keys = new Dictionary<string, List<long>>();

            if (!approx) {
                //group for keys
                for (var i = 0; i < yt.Rows[0].val.GetCount(); i++) {
                    var keyVal = yt.Rows[keyIdx].val.GetString(i).ToUpper();
                    List<long> keyIndices = null;
                    if (!keys.TryGetValue(keyVal, out keyIndices)) {
                        keyIndices = new List<long>();
                        keys[keyVal] = keyIndices;
                    }
                    keyIndices.Add(i);
                }

            }
            
            if (newVal.GetType() != typeof(A<JTable>)) {
                for (var i = 0; i < newVal.GetCount(); i++) {
                    List<long> keyIndices = null;
                    var checkVal = yb.Ravel[1].val.GetString(i).ToUpper();
                    var newValx = newVal.GetVal(i);
                    if (keys.TryGetValue(checkVal, out keyIndices)) {
                        for (var k = 0; k < keyIndices.Count; k++) {
                            yt.Rows[idx].val.SetVal(keyIndices[k], newValx);
                        }
                    }
                }
            }
            else { //join two tables
                var xt = (newVal as A<JTable>).First();
                string keyColumn = yb.Count == 1 ? yb.Ravel[0].val.ToString() : yb.Ravel[1].val.ToString();

                //TODO: need a smarter way to deal with this
                bool noOrig = yb.Count >= 3 && yb.Ravel[2].val.ToString() == "[";
                string lookupCol = yb.Count >= 3 && !noOrig ? yb.Ravel[2].val.ToString() : null;

                var joinKeyIdx = Array.IndexOf(xt.Columns, keyColumn);

                if (approx) {
                    var testVal = yt.Rows[keyIdx].val;
                    if (testVal.GetType() == typeof(A<long>))
                        return lookupApprox<long>(xt, joinKeyIdx, yt, keyIdx);
                    else if (testVal.GetType() == typeof(A<decimal>))
                        return lookupApprox<decimal>(xt, joinKeyIdx, yt, keyIdx);
                    else if (testVal.GetType() == typeof(A<double>))
                        return lookupApprox<double>(xt, joinKeyIdx, yt, keyIdx);
                    throw new NotSupportedException();
                }
                var joinKeys = new Dictionary<string, long>();
                for (var i = 0; i < xt.RowCount; i++) {
                    var keyVal = xt.Rows[joinKeyIdx].val.GetString(i).ToUpper();
                    joinKeys[keyVal] = i;
                }
                var newCols = xt.Columns.Where(xv => xv != keyColumn && (xv == lookupCol || lookupCol == null)).ToArray();
                var newRows = new Box[newCols.Length];
                for (var i = 0; i < newCols.Length; i++) {
                    var xtIdx = Array.IndexOf(xt.Columns, newCols[i]);
                    var indices = new long[yt.RowCount];
                    foreach(var kv in keys) {
                        long newIdx;
                        if (!joinKeys.TryGetValue(kv.Key, out newIdx)) {
                            newIdx = -1;
                        }
                        foreach (var n in kv.Value) {
                            indices[n] = newIdx;
                        }

                    }
                    newRows[i] = xt.Rows[xtIdx].val.FromIndices(indices).Box();
                    //newRows[i] = new A<long>(yt.RowCount).Box();
                }
                var newT = new JTable { Columns = newCols, Rows = newRows };
                if (!noOrig && lookupCol == null) {
                    yt = Verbs.linktable(yt.WrapA(), newT.WrapA()).First();
                }
                else {
                    return newT.WrapA();
                }
                
            }
            
            if (yt.ColumnExpressions != null) {
                foreach (var kv in yt.ColumnExpressions) {
                    var locals = new Dictionary<string, AType>();

                    if (Conjunctions.Parser.LocalNames != null) {
                        foreach (var kvx in Conjunctions.Parser.LocalNames) {
                            locals[kvx.Key] = kvx.Value;
                        }
                    }

                    for (var i = 0; i < yt.Columns.Length; i++) {
                        locals[JTable.SafeColumnName(yt.Columns[i])] = yt.Rows[i].val;
                    }
                    var expressionResult = Conjunctions.Parser.exec(kv.Value, locals);
                    int colIdx = Array.IndexOf(yt.Columns, kv.Key);
                    yt.Rows[colIdx] = new Box { val = expressionResult };
                }
            }
            
            return yt.WrapA();
        }
        public AType Call1(AType method, AType y) {
            var verbs = ((A<Verb>)method);
            var verb = ((A<Verb>)method).Ravel[0];
            var adverb = verb.adverb;
            var op = verb.op;

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };

            if (verb.childAdverb != null) {
                newVerb.Ravel[0].adverb = verb.childAdverb;
            }

            if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;
                newVerb.Ravel[0] = cv;
            }

            //todo: hack to support train in adverb
            if (verbs.Count > 1) {
                newVerb = new A<Verb>(verbs.Count-1);
                for (var i = 1; i < verbs.Count; i++) {
                    newVerb.Ravel[(i-1)] = verbs.Ravel[i];
                }
            }
            
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
            else if (adverb == "/" && op == "+" && y.Rank == 1 && y.GetType() == typeof(A<decimal>)) {
                return reduceplus((A<decimal>)y);
            }
            else if (adverb == "\\" && op == "+" && newVerb.Ravel[0].adverb == "/" && y.GetType() == typeof(A<decimal>)) {
                return runsum((A<decimal>)y);
            }
            else if (adverb == "\\" && op == "+" && newVerb.Ravel[0].adverb == "/" && y.GetType() == typeof(A<long>)) {
                return runsum((A<long>)y);
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
                else if (y.GetType() == typeof(A<decimal>)) {
                    return reduce<decimal>(newVerb, (A<decimal>)y);
                }
                else if (y.GetType() == typeof(A<Box>)) {
                    return reduce<Box>(newVerb, (A<Box>)y);
                }
                else if (y.GetType() == typeof(A<JTable>)) {
                    //this is a way to query a table
                    return Verbs.beheadTable(keyTable(method, (A<JString>)null, (A<JTable>)y) as A<JTable>);
                }
            }
            else if (adverb == "~") {
                return Verbs.InvokeExpression("reflex", y, y, 1, this, newVerb);
            }
            else if (adverb == "/.") {
                if (y.GetType() == typeof(A<JTable>)) {
                    var table = Verbs.beheadTable(keyTable(method, (A<JString>)null, (A<JTable>)y) as A<JTable>);
                    if (table.GetCount() == 1) {
                        return table.GetValA(0);
                    }                    
                    return table;
                }
            }
            else if (adverb == "}") {
                if (y.GetType() == typeof(A<JTable>)) {
                    return setTableProps((AType)verb.childNoun, (A<JTable>)y);
                }
            }
            throw new NotImplementedException(adverb + " on y:" + y + " type: " + y.GetType());
        }

        public AType Call2(AType method, AType x, AType y) {
            var verb = ((A<Verb>)method).Ravel[0];
            var adverb = verb.adverb;
            var op = verb.op;
           

            //create a new verb without the adverb component so we can safely pass it around
            var newVerb = new A<Verb>(0);
            newVerb.Ravel[0] = new Verb { op = op };
            if (verb.childVerb != null) {
                Verb cv = (Verb)verb.childVerb;                
                newVerb.Ravel[0] = cv;
            }

            if (verb.childAdverb != null) {
                newVerb.Ravel[0].adverb = verb.childAdverb;
            }
            if (verb.childNoun != null) {
                newVerb.Ravel[0].childNoun = verb.childNoun;
            }

            newVerb.Ravel[0].conj = verb.conj;
            newVerb.Ravel[0].rhs = verb.rhs;

            var verbs = ((A<Verb>)method);
            //todo: hack to support train in adverb
            if (verbs.Count > 1) {
                newVerb = new A<Verb>(verbs.Count - 1);
                for (var i = 1; i < verbs.Count; i++) {
                    newVerb.Ravel[(i - 1)] = verbs.Ravel[i];
                }
            }

            if (adverb == "/" && y.GetType() == typeof(A<long>) && x.GetType() == typeof(A<long>)) {
                return table(newVerb, (A<long>)x, (A<long>)y);
            }
            else if (adverb == "/.") {
                if (y.GetType() == typeof(A<JTable>)) {
                    return Verbs.InvokeExpression("keyTable", x, y, 2, this, newVerb);
                }
                else {
                    return Verbs.InvokeExpression("key", x, y, 2, this, newVerb);
                }
                
            }
            else if (adverb == "}") {
                if (y.GetType() == typeof(A<JTable>)) {
                    return amendTable((AType)verb.childNoun, x, (A<JTable>)y);
                }
            }
            throw new NotImplementedException("ADV: " + adverb + " on x:" + x + " y:" + y + " type: " + y.GetType());
        }

    }

    public static class Primes {
        public static bool IsPrime(long n) {
            if (n <= 1)
                return false; //should fail?

            if (n == 2)
                return true;

            if (n % 2 == 0)
                return false;

            for (int i = 3; i < Math.Sqrt(n) + 1; i += 2) {
                if (n % i == 0) {
                    return false;
                }
            }
            return true;
        }

        public static long GetNthPrime(long n) {

            if (n < 0)
                return 0;// TODO throw an exception ??
            if (n == 0)
                return 2;
            long prime = 2;
            long i = 0;

            long count = 3; // start at 3
            while (i < n) {
                if (IsPrime(count) == true) {
                    prime = count;
                    i++;
                }
                count += 2;
            }
            return prime;
        }

        public static long Pi(float n) {
            if (n < 2)
                return 0;
            if (n < 3)
                return 1;
            if (n < 5)
                return 2;
            if (n < 7)
                return 3;
            if (n < 11)
                return 4;
            long c = Pi((float)Math.Pow(n, 1.0f / 3));

            long mu = Pi((float)Math.Sqrt(n)) - c;
            return (phi(n, c) + (long)(c * (mu + 1) + (mu * mu - mu) * 0.5f) - 1 - SumPi(n, c, mu));
        }

        private static long SumPi(float m, long n, long mu) {
            long i = 1;
            long total = 0;
            while (i <= mu) {
                total += Pi(m / GetNthPrime(n + i - 1));
                i++;
            }
            return total;
        }

        private static long phi(float m, long n) {
            if (m < 0 || n < 0)
                throw new System.Exception("Arguments must be non-negative");
            if (n == 0) {
                return (long)m;
            }
            else {
                return phi(m, n - 1) - phi((long)(m / (float)(GetNthPrime(n - 1))), n - 1);
            }
        }

        public static long[] Factor(long n) {
            List<long> lst = new List<long>();
            if (n <= 1)
                return lst.ToArray(); //throw exception?

            long divisor = PollardRho(n, new Random());
            lst.Add(divisor);
            lst.AddRange(Factor(n / divisor));
            return lst.ToArray();
        }

        //can be used for 2 p: y, possibly.
        public static Dictionary<long, int> GetFactorExponents(List<long> factorList) {
            Dictionary<long, int> d = new Dictionary<long, int>();
            foreach (long l in factorList) {
                if (d.ContainsKey(l))
                    d[l]++;
                else
                    d[l] = 1;
            }
            return d;
        }

        public static List<long> GetFactorsWithExponents(List<long> factorList) {
            Dictionary<long, int> exponentDict = GetFactorExponents(factorList);
            List<long> keys = new List<long>();
            List<long> vals = new List<long>();
            foreach (KeyValuePair<long, int> kvp in exponentDict) {
                keys.Add(kvp.Key);
                vals.Add((long)kvp.Value);
            }
            keys.AddRange(vals);
            return keys;
        }


        //unused.
        private static long FactorizeSimple(long n, long previous) {
            if (n % 2 == 0)
                return 2;
            if (n % 3 == 0)
                return 3;
            if (n % 5 == 0)
                return 5;

            long i;
            for (i = previous; i < Math.Sqrt(n) + 1; i += 2) {
                if (n % i == 0)
                    return i;
            }
            return n;
        }

        private static long PollardRho(long n, Random rand) {

            if (n % 2 == 0)
                return 2;
            if (n % 3 == 0)
                return 3;
            if (n % 5 == 0)
                return 5;
            byte[] buffer = BitConverter.GetBytes(n);
            rand.NextBytes(buffer);
            long summand = BitConverter.ToInt64(buffer, 0);
            rand.NextBytes(buffer);
            long a = BitConverter.ToInt64(buffer, 0);
            long b = a;
            long divisor;
            //
            a = (n + a * a + summand) % n;
            b = (((n + b * b + summand) % n) * (n + (n + b * b + summand) % n) + summand) % n;
            divisor = GCD(a - b, n);

            while (divisor == 1) {
                a = (a * a + summand) % n;
                b = (((n + b * b + summand) % n) * (n + (n + b * b + summand) % n) + summand) % n;
                divisor = GCD(a - b, n);
            }
            return divisor;
        }

        private static long GCD(long a, long b) {
            return b == 0 ? a : GCD(b, (b + a) % b);
        }

        public static List<long> atkinSieve(int n) {
            if (n <= 1) {
                List<long> l = new List<long>();
                return l;
            }
            int sqrt = (int)(Math.Sqrt((double)n) + 1);
            bool[] primes = new bool[n]; //initialize list all false

            for (int i = 0; i < sqrt; i++) {
                for (int j = 0; j < sqrt; j++) {
                    int s = 4 * i * i + j * j;// 4i^2 +j^2
                    if (s < n && (s % 12 == 1 || s % 12 == 5)) {
                        primes[s] = !primes[s];
                    }
                    s = 3 * i * i + j * j;// 3i^2 +j^2
                    if (s < n && s % 12 == 7) {
                        primes[s] = !primes[s];
                    }
                    s = 3 * i * i - j * j; // 3i^2 - j^2
                    if (s < n && i > j && s % 12 == 11) {
                        primes[s] = !primes[s];
                    }
                }
            }

            List<long> result = new List<long>();
            result.Add(2L);
            result.Add(3L);
            long N = (long)n; //to prevent int overflow
            for (int i = 2; i < n; i++) {
                int j = 0;
                long I = (long)i;
                long J = (long)j;
                if (primes[i] == true) {
                    long K = I * I + I * J;
                    while (K < N && K <= Int32.MaxValue) {
                        primes[(int)K] = false;
                        j++;
                        J = (long)j;
                        K = I * I + I * J;
                    }
                    result.Add(I);
                }
            }
            return result;
        }

    }

    public static class Gamma {
        static int g = 7;
        static double[] p = {0.99999999999980993, 676.5203681218851, -1259.1392167224028,
	     771.32342877765313, -176.61502916214059, 12.507343278686905,
	     -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7};

        public static double GammaReal(double y) {
            y += 1;
            if (y < 0.5)
                return Math.PI / (Math.Sin(Math.PI * y) * GammaReal(1 - y));
            else {
                y -= 1;
                double x = p[0];
                for (var i = 1; i < g + 2; i++) {
                    x += p[i] / (y + i);
                }
                double t = y + g + 0.5;
                return Math.Sqrt(2 * Math.PI) * (Math.Pow(t, y + 0.5)) * Math.Exp(-t) * x;
            }
        }
    }

}

