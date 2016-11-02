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


//building on mono: mcs microj.cs primitives.cs repl.cs /define:CSSCRIPT /r:bin/CSScriptLibrary
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
using System.Text.RegularExpressions;
#endif

namespace MicroJ
{
    public class DomainException : Exception {

    }
    public abstract class AType
    {
        public long[] Shape;
        public int Rank { get { return Shape == null ? 0 : Shape.Length; } }
        public Type Type;
        public Types TypeE;

        public enum Types {
            Double,
            Decimal,
            Long,
            JString,
            Box,
            Bool
        }
        public AType(Type t) {
            Type = t;
            if (t == typeof(Decimal)) {
                this.TypeE = Types.Decimal;
            } 
            else if (t == typeof(Double)) {
                this.TypeE = Types.Double;
            }
            else if (t == typeof(long)) {
                this.TypeE = Types.Long;
            }
            else if (t == typeof(bool)) {
                this.TypeE = Types.Bool;
            }
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
                newShape = shape.ToList().Concat(new long[] {ncells, maxCt}).ToArray();
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
                    z.Shape = new long[] {frame[0], zs[0].Length / frame[0] };
                }
                z.Ravel = zs[0];
                return z;
            }
            else
                return Fill(zs, y.Shape, frame);
        }


        public abstract string GetString(long n, string format);
        public abstract string GetString(long n);
        public abstract int GetHashCode(long n);
        public abstract long GetCount();
        public abstract bool SliceEquals(long offseta, long offsetb, long count);        
        public abstract AType Merge(long[] newShape, AType[] vs);
        public abstract AType Create(long[] newShape);
        public abstract long[] GradeUp();
        public abstract void SetVal(long n, object val);
        public abstract object GetVal(long n);
        public abstract AType GetValA(long n);
        public abstract AType FromIndices(long[] indices, bool flattenStrings = true);
        public abstract AType TryConvertLong(Parser parse);
        public abstract bool IsAtom();
        public abstract long GetLong(int n);
        public abstract bool CanBeInt();
        
        public JString GetCharJString(long n) {
            return new JString { str = GetChar(n) };
        }

        
        
        public string GetChar(long n) {
            var cells = (long) Shape[Shape.Length-1];
            long idx = (long)Math.Floor((double)(n / cells));
            long remainder = (long) n % cells;
            var str = ((A<JString>)this).Ravel[idx].str;
            if (remainder < str.Length) {
                return str[(int)remainder].ToString();
            }
            else {
                return " ";
            }
        }

        public long ShapeProduct(int skip=0, int drop=0) {
            if (Shape == null) { return 0;  }
            return ShapeProduct(Shape, skip, drop);
        }

        public static long ShapeProduct(long[] ri, int skip=0, int drop=0) {
            return ri.Skip(skip).Take(ri.Length-skip-drop).Aggregate(1L, (prod, next) => prod * next);
        }

        public long[] ShapeCopy() {
            return (long[])Shape.Clone();
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

        public A<long> ConvertLong() {
            if (Type == typeof(long)) {
                return (A<long>)this;
            }
            else if (Type == typeof(bool)) {
                var a = (A<bool>)this;
                var z = new A<long>(a.Count, a.Shape);
                z.Ravel = a.Ravel.Select(x =>(long) ( x == true ? 1 : 0)).ToArray();
                return z;
            }
            else if (Type == typeof(decimal)) {
                var a = (A<decimal>)this;
                var z = new A<long>(a.Count, a.Shape);
                z.Ravel = a.Ravel.Select(x => (long)x).ToArray();
                return z;
            }
            throw new NotImplementedException();
        }

        public static AType MakeA(long val) {
            return new A<long>(0) { Ravel = new long[] { val } };
        }
        public static AType MakeA(string word, Dictionary<string, AType> names, Dictionary<string, AType> locals = null) {
            int val;
            double vald;
            bool UseDecimal = names != null && names.ContainsKey("UseDecimal");

            if (word.Length > 1 && word[0] == '_' && char.IsDigit(word[1])) {
                word = word.Replace("_", "-");
            }

            if (locals != null && locals.ContainsKey(word)) {
                return locals[word];
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
            //backtick to defer resolution to name resolver
            else if (word.StartsWith("`") && Parser.NameResolver != null) {
                var aval = Parser.NameResolver(word.TrimStart('`'));
                return aval != null ? aval : new A<Undefined>(0);
            }
            if (word.Contains(" ") && !word.Contains(".") && word != "_" && !word.Contains(" _ ")) {
                var longs = new List<long>();
                foreach (var tpart in word.Split(' ')) {
                    string part = tpart;

                    if (part.Length > 1 && part[0] == '_' && char.IsDigit(part[1])) {
                        part = part.Replace("_", "-");
                    }
                    
                    longs.Add(int.Parse(part, CultureInfo.InvariantCulture));
                    
                }

                if (UseDecimal) {
                    var at = new A<decimal>(longs.Count);
                    at.Ravel = longs.Select(xv=>Convert.ToDecimal(xv)).ToArray();
                    return at;
                }
                else {
                    var a = new A<long>(longs.Count);
                    a.Ravel = longs.ToArray();
                    return a;
                }

            }
            else if ((word.Contains(" ") && (word.Contains(".")) || word.Contains(" _ ") || word == "_")) {
                var doubles = new List<double>();
                foreach (var part in word.Split(' ')) {
                    if (part == "_") {
                        doubles.Add((double)Parser.DecimalInfinity);
                    }
                    else {
                        double d = 0;
                        if (double.TryParse(part, out d)) {
                            doubles.Add(d);
                        }
                        else {
                            doubles.Add((double)Parser.DecimalInfinity);
                        }                        
                    }
                    
                }

                if (UseDecimal) {
                    var at = new A<decimal>(doubles.Count);
                    at.Ravel = doubles.Select(xv => Convert.ToDecimal(xv)).ToArray();
                    return at;
                }
                else {
                    var a = new A<double>(doubles.Count);
                    a.Ravel = doubles.ToArray();
                    return a;
                }
                
            }
                //allow using . to force cast to double
            else if (!word.Contains(".") && int.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) {
                if (UseDecimal) {
                    var a = new A<decimal>(0);
                    a.Ravel[0] = Convert.ToDecimal(val);
                    return a;
                }
                else {
                    var a = new A<long>(0);
                    a.Ravel[0] = val;
                    return a;
                }
                
            }
            else if (double.TryParse(word, NumberStyles.Any, CultureInfo.InvariantCulture, out vald)) {
                if (UseDecimal) {
                    var a = new A<decimal>(0);
                    a.Ravel[0] = Convert.ToDecimal(vald);
                    return a;
                }
                else {
                    var a = new A<double>(0);
                    a.Ravel[0] = vald;
                    return a;
                }                
            }
            else if (Verbs.Words.Contains(word)) {
                var a = new A<Verb>(1);
                a.Ravel[0] = new Verb { op = word };
                return a;
            }
            else if (Parser.NameResolver != null) {
                var aval = Parser.NameResolver(word);
                return aval != null ? aval : new A<Undefined>(0);
            }
            return new A<Undefined>(0);
        }

        public Box Box() {
            return new Box { val = this };
        }
        public AType UnBox() {
            return (this as A<Box>).Ravel[0].val;
        }
    }

    public struct Undefined { }

    public struct Verb {
        public string op;
        public string adverb;
        public string conj;
        public string rhs;
        public object childVerb;
        public object childNoun;
        public string childAdverb;
        public string explicitDef;

        public override string ToString() {
            string str = "";
      
            if (childVerb != null) {
                str = str + "(" + childVerb.ToString() + ")";
            }

         

            if (explicitDef != null) {
                str = explicitDef;
            }

            if (op != null) str += op;
            if (conj != null) str += " " + conj;
            if (rhs != null) str += " " + rhs;

            if (childAdverb != null) {
                str = str + childAdverb.ToString();
            }

            if (adverb != null) str += " " + adverb;
            
            return str;
        }
        public AType WrapA() {
            return new A<Verb>(0) { Ravel = new Verb[] { this } };
        }
    }

    //tbd should we use chars instead?
    public struct JString : IComparable {
        public string str;
        
        public override string ToString() {
            //todo: determine if this is a good idea, needed for tests for now
            return str.Replace("\\n", "\n");
        }
        public int CompareTo(object obj) {
            return str.CompareTo(((JString)obj).str);
        }
        public AType WrapA() {
            return new A<JString>(new long[] { str.Length }) { Ravel = new JString[] { this } };
        }
    }

    public struct Box {
        public AType val;
        public override string ToString() {
            var vt = val.ToString();
            var rep = 1;
            if (val.Shape != null && val.Shape.Length > 0) {
                rep = (int) val.Shape[val.Shape.Length-1];
            }
            var sep = "+" + new String('-', rep) + "+";
            return sep + "\n|" + vt + "|\n" + sep;
        }
        
        //needed for nub
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }
        public static A<Box> BoxLongs(params long[] vals) {
            var ret = new Box[vals.Length];
            for (var i = 0; i < vals.Length; i++) {
                ret[i].val = new A<long>(0) { Ravel = new long[] { vals[i] } };
            }
            return new A<Box>(vals.Length) { Ravel = ret };
        }
        public A<Box> WrapA() {
            return new A<Box>(0) { Ravel = new Box[] { this } };
        }
    }

    public struct JTable {
        public string[] Columns;
        public Box[] Rows;
        public long offset;
        public long take;
        public long[] indices;
        public Dictionary<string, string> ColumnExpressions;
        public Dictionary<string, string> FooterExpressions;
        public Dictionary<string, long> UniqueKeys;
        public Dictionary<string, List<long>> Key;

        public IEnumerator<dynamic> GetEnumerator() {
            JTable yt = this;
            for (var i = 0; i < yt.RowCount; i++) {
                var idx = yt.indices != null ? yt.indices[i] : i;
                var obj = new System.Dynamic.ExpandoObject();
                var objD = obj as IDictionary<string, object>;
                for (var k = 0; k < yt.Columns.Length; k++) {
                    var val = yt.Rows[k].val.GetVal(idx);
                    if (val.GetType() == typeof(JString)) {
                        objD[yt.Columns[k]] = val.ToString();
                    }
                    else {
                        objD[yt.Columns[k]] = val;
                    }
                }
                yield return obj;
            }
        }

        public List<Dictionary<string, object>> ToJson() {
            var yt = this;

            var rows = new List<Dictionary<string, object>>();
            for (var i = 0; i < yt.RowCount; i++) {
                var idx = yt.indices != null ? yt.indices[i] : i;
                var row = new Dictionary<string, object>();
                for (var k = 0; k < yt.Columns.Length; k++) {
                    var val = yt.Rows[k].val.GetVal(idx);
                    if (val.GetType() == typeof(JString)) {
                        row[yt.Columns[k]] = val.ToString();
                    }
                    else if (val.GetType() == typeof(byte)) {
                        row[yt.Columns[k]] = yt.Rows[k].val.GetString(idx);
                    }
                    else {
                        row[yt.Columns[k]] = val;
                    }

                }
                rows.Add(row);
            }

            if (yt.FooterExpressions != null) {
                var locals = new Dictionary<string, AType>();
                var self = yt;

                Action<bool> buildFooter = (filtered) => {
                    var row = new Dictionary<string, object>();
                    for (var i = 0; i < self.Columns.Length; i++) {
                        locals[self.Columns[i]] = self.Rows[i].val;
                        locals["_C" + i] = locals[self.Columns[i]];
                    }
                    foreach (var col in self.Columns) {
                        if (self.FooterExpressions.ContainsKey(col)) {
                            var expressionResult = new Parser().exec(self.FooterExpressions[col], locals);

                            if (expressionResult.GetType() == typeof(JString)) {
                                row[col] = expressionResult.ToString();
                            }
                            else {
                                row[col] = expressionResult.GetVal(0);
                            }
                        }
                        else {
                            row[col] = "";
                        }

                    }
                    rows.Add(row);
                };

                buildFooter(true);
            }
            return rows;
        }
        public static string SafeColumnName(string col) {
            return Regex.Replace(col, @"[^A-Za-z0-9\\_]+", "");
        }
        public long RowCount {
            get {
                if (indices == null) {
                    //return Rows[0].val.Shape[0];
                    return Rows[0].val.GetCount();
                }
                else {
                    return indices.Length;
                }
            }
        }
        public A<JTable> WrapA() {
            return new A<JTable>(1) { Ravel = new JTable[] { this } }; 
        }
        public int GetColIndex(AType y) {
            int colIdx = -1;
            if (y.GetType() == typeof(A<JString>)) {
                var colStr = y.GetString(0);
                var renames = colStr.Split(new string[] {Parser.COLUMN_ALIAS_KEYWORD }, StringSplitOptions.RemoveEmptyEntries);
                if (renames.Length > 1) {
                    colStr = renames[1].Trim();
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                colIdx = Array.IndexOf(Columns, colStr);
                if (colIdx == -1) {
                    colIdx = Array.IndexOf(Columns.Select(x=>rgx.Replace(x,"")).ToArray(), rgx.Replace(colStr,""));
                }
                if (colIdx == -1) {
                    colStr = y.GetString(0);
                    colIdx = Array.IndexOf(Columns.Select(x => rgx.Replace(x, "")).ToArray(), rgx.Replace(colStr, ""));
                }
            }
            else if (y.GetType() == typeof(A<Box>)) {
                colIdx = -2;
            }
            else if (y.GetType() == typeof(A<decimal>)) {
                colIdx = (int)(y as A<decimal>).Ravel[0];
            }
            else {
                colIdx = (int) (y as A<long>).Ravel[0];
            }
            if (colIdx == -1) {
                throw new ArgumentException("Column " + y + " not found");
            }
            return colIdx;
        }

        public JTable Clone() {
            return new JTable {
                Columns = Columns.Select(x=>x).ToArray(),
                Rows = Rows.Select(x=>x).ToArray(),
                offset = offset,
                take = take,
                indices = indices == null ? null : indices.Select(x=>x).ToArray(),
                ColumnExpressions = ColumnExpressions,
                FooterExpressions = FooterExpressions
            };
        }

        public static JTable FromDict(A<Box> dict) {
            var t = new JTable();
            var columns = new string[dict.Shape[1]];
            for (var i = 0; i < dict.Shape[1]; i++) {
                columns[i] = dict.Ravel[i].val.GetString(0);
            }
            var rows = new Box[dict.Shape[1]];
            int offset = 0;
            for (var i = dict.Shape[1]; i < dict.Count; i++) {
                rows[offset++] = dict.Ravel[i].val.Box();
            }
            t.Columns = columns;
            t.Rows = rows;
            return t;
        }
            
        public override string ToString() {
            var ct = Rows[0].val.GetCount();
            if (indices != null) {
                ct = indices.Length;
            }

            var ttake = take;
            if (take == 0) {
                ttake = ct;
            }
            var footer = FooterExpressions == null ? 0 : 1;
            if (footer != 0 && (indices != null || offset != 0 || ttake != ct)) {
                footer++;
            }

            var newShape = new long[] {ttake+1+footer , Columns.Length};
            var formatter = new Formatter(newShape, "");
            foreach (var col in Columns) {
                int rep = col.Length;
                var sep = "+" + new String('-', rep) + "+";
                var valStr = sep + "\n|" + col + "|\n" + sep;

                formatter.Add(valStr);
            }

            var newIndices = new List<long>();
            for (var i = offset; i < (offset+ttake) && i < ct && i < Parser.OUTPUT_MAX_ROWS; i++) {
                if (indices == null) {
                    newIndices.Add(i);
                }
                else {
                    newIndices.Add(indices[i]);
                }
                
                for (var k = 0; k < Columns.Length; k++) {
                    var val = Rows[k].val;

                    string vt = null;
                    if (indices == null) {
                        vt = val.GetString(i);
                    }
                    else {
                        vt = val.GetString(indices[i]);
                    }
                    
                    var rep = 1;
                    
                    var sep = "+" + new String('-', rep) + "+";
                    var valStr = sep + "\n|" + vt + "|\n" + sep;

                    formatter.Add(valStr);
                }
                    
            }
            if (FooterExpressions != null) {
                var locals = new Dictionary<string, AType>();
                var self = this;
                
                Action<bool> buildFooter = (filtered) => {
                    for (var i = 0; i < self.Columns.Length; i++) {
                        if (!filtered || (self.indices == null && ttake == ct && self.offset == 0)) {
                            locals[self.Columns[i]] = self.Rows[i].val;
                            locals["_C" + i] = locals[self.Columns[i]];
                        }
                        else {
                            locals[self.Columns[i]] = self.Rows[i].val.FromIndices(newIndices.ToArray());
                            locals["_C" + i] = locals[self.Columns[i]];
                        }
                    }
                    foreach (var col in self.Columns) {
                        string val = "";
                        if (self.FooterExpressions.ContainsKey(col)) {
                            var expressionResult = new Parser().exec(self.FooterExpressions[col], locals);
                            val = expressionResult.ToString();
                        }

                        //int rep = col.Length;
                        //var sep = "+" + new String('-', rep) + "+";
                        var valStr = "+-+\n|" + val + "|\n+-+";
                        formatter.Add(valStr);
                    }
                };
                buildFooter(true);
                if (footer == 2) { buildFooter(false); }
            }
            return formatter.ToString();
        }
    }
    public class A<T> : AType where T : struct {

        public T[] Ravel;


        public override long GetLong(int n) {
            return Convert.ToInt64(Ravel[n]);
        }

        public override bool CanBeInt() {
            if (GetType() == typeof(A<long>)|| GetType() == typeof(A<bool>)) {
                return true;
            }
            if (IsAtom() || Count == 1) {
                long tryLong;
                if (Int64.TryParse(GetString(0), out tryLong)) return true;
            }
            return false;
        }

        public long Count { get { return Ravel == null ? 1 : Ravel.Length; } }

        public static Func<T, T, T> AddFunc;
        public static Func<T, T, bool> EqualFunc;

        public T First() { return Ravel[0];  }

        public override bool IsAtom() {
            return Rank == 0 || (Rank == 1 && this.GetType() == typeof(A<JString>) && Ravel.Length == 1);
        }

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

        //not used, but here for the future
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FastEquals(T a, T b) {
            if (EqualFunc == null) {
                var par1 = Expression.Parameter(typeof(T));
                var par2 = Expression.Parameter(typeof(T));

                var add = Expression.Equal(par1, par2);

                EqualFunc = Expression.Lambda<Func<T, T, bool>>(add, par1, par2).Compile();
            }
            return EqualFunc(a, b);
        }

        public A()
            : base(typeof(T)) {

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

        public A(long[] shape) : base(typeof(T)) {
            if (typeof(T) != typeof(JString)) {
                var n = ShapeProduct(shape);
                Ravel = new T[n];
                Shape = shape;
            }
            else {
                var n = ShapeProduct(shape, drop:1);
                Ravel = new T[n];
                Shape = shape;
            }
        }

        public override AType TryConvertLong(Parser parser) {
            if (parser.UseDecimal && this.GetType() == typeof(A<decimal>)) {
                A<long> retA;
                if (this.Shape != null) {
                    retA = new A<long>(this.Shape);
                }
                else {
                    retA = new A<long>(0);
                }                
                retA.Ravel = this.Ravel.Select(x=>Convert.ToInt64(x)).ToArray();
                return retA;
            }
            return this;
        }
        public override bool SliceEquals(long offseta, long offsetb, long count) {
            for (long i = 0; i < count; i++) {
                if (!Ravel[offseta + i].Equals(Ravel[offsetb + i])) {
                    return false;
                }
            }
            return true;
        }

        public override void SetVal(long n, object val) {
            Ravel[n] = (T)val;
        }

        public override object GetVal(long n) {
            return Ravel[n];
        }

        public override AType GetValA(long n) {
            return new A<T>(0) { Ravel = new T[] { Ravel[n] } };
        }

        public override AType FromIndices(long[] indices, bool flattenStrings = true) {

            A<T> v = null;
            if (typeof(T) == typeof(JString) && !flattenStrings) {
                //if (indices.Length > 1) {
                    var newShape = new long[] { indices.Length, 1 };
                    v = new A<T>(indices.Length, newShape);
                /*
                }
                else {
                    //atom of null
                    v = new A<T>(0);
                }
                 */
                
            }
            else {
                v = new A<T>(indices.Length);
            }
            //v = new A<T>(indices.Length);

            for(var i = 0; i < indices.Length;i++) {
                var idx = indices[i];
                if (idx != -1) {
                    v.Ravel[i] = Ravel[indices[i]];
                }
                else {
                    //todo speed up
                    if (typeof(T) == typeof(JString)) {
                        v.Ravel[i] = (T) (object) new JString { str = "" };
                    }
                    else {
                        v.Ravel[i] = default(T);
                    }
                    
                }
                
            }
            return v;
        }
        //creates a new value from an array of values
        public override AType Merge(long[] newShape, AType[] vs) {
            var v = new A<T>(newShape);
            long offset = 0;
            for (var i = 0; i < vs.Length; i++) {
                var a = (A<T>)vs[i];
                if (a.Count == 0) {
                    v.Ravel[offset++] = default(T);
                }
                else {
                    for (var k = 0; k < a.Count; k++) {
                        v.Ravel[offset++] = a.Ravel[k];
                    }
                }                
            }
            return v;
        }

        public override AType Create(long[] newShape) {
            var v = new A<T>(Adverbs.prod(newShape), newShape);
            return v;
        }
        public string StringConverter(T val) {
            var str = "";
            if (typeof(T) == typeof(Box)) {
                Box box = ((Box)(object)val);

                var cells = box.val.ToString().Split('\n');
                var maxCell = cells.Max(x => x.Length);
                var sep = "+" + String.Join("+", new String('-', maxCell)) + "+" + "\n";
                var ret = sep + String.Join("", cells.Select(x => "|" + x.PadRight(maxCell) + "|" + "\n").ToArray()) + sep;
                //chop trailing \n
                return ret.Substring(0, ret.Length - 1);
            }
            else if (typeof(T) == typeof(Byte)) {
                return System.Text.Encoding.UTF8.GetString((Byte[])(object)Ravel);
            }
            else if (typeof(T) == typeof(bool)) {
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
            else if (typeof(T) == typeof(decimal)) {
                decimal v = (decimal)(object)val;
                if (v == Parser.DecimalInfinity) {  return "_";}
                if (v < 0) { return "_" + Math.Abs(v); }
                else { return v.ToString(); }
            }
            else if (typeof(T) == typeof(double)) {
                double v = (double)(object)val;
                if (v < 0) { return "_" + Math.Abs(v); }
                else if (double.IsInfinity(v) || v == (double)Parser.DecimalInfinity) { return "_"; }
                else if (v > 0) { return v.ToString(CultureInfo.InvariantCulture); }
                else if (double.IsNaN(v)) { return "0"; }
                else { return v.ToString(CultureInfo.InvariantCulture); }
            }
            else if (typeof(T) == typeof(JString)) {
                JString v = (JString)(object)val;
                return v.str != null ? v.str : "";
            }
            else {
                str = val.ToString();
            }
            return str;
        }
        
        public override string GetString(long n) {
            if (Type == typeof(Decimal) && Parser.MAX_DECIMAL >= 0) {
                var val = Convert.ToDecimal(Ravel[n]);
                if (Math.Truncate(val) == val) {
                    return val.ToString();
                } else {
                    return val.ToString("N" + Parser.MAX_DECIMAL);
                }                
            }
            if (Type != typeof(Byte)) {
                return Ravel[n].ToString();
            }
            else {
                int offset = (int)(Shape[1] * n);
                return System.Text.Encoding.UTF8.GetString(Ravel as Byte[], (int)offset, (int)Shape[1]);                
            }
            
        }

        public override string GetString(long n, string format) {
            return String.Format(format, Ravel[n]);
        }

        public override int GetHashCode(long n) {
            return Ravel[n].GetHashCode();
        }

        public override long[] GradeUp() {
            var indices = new long[Count];
            for (long i = 0; i < indices.Length; i++) {
                indices[i] = i;
            }
            return indices.OrderBy(k => Ravel[k]).ToArray();
        }

        public override long GetCount() {
            return Count;   
        }
        public override string ToString() {
            if (Ravel == null) { return ""; }
            else if (typeof(T) == typeof(JString)) {
                var ravel = ((JString[])(object)Ravel);
                if (Shape == null) {
                    //for 0 : 0 
                    return ((JString[])(object)Ravel)[0].str;
                } else if (ravel != null && ravel.Length == 1) {
                    return ravel[0].ToString();
                }
                var maxLen = 0;
                foreach(var jstr in ravel) {
                    if (jstr.str.Length > maxLen) { maxLen = jstr.str.Length; }
                }
                var newShape = new long[] { Shape[0], maxLen };
                if (Rank > 2) {
                    newShape = Shape.Concat(new long[] { maxLen }).ToArray();
                }
                var formatter = new Formatter(newShape, "");
                foreach (var jstr in ravel) {
                    for (var i = 0; i < maxLen; i++) {
                        if (i < jstr.str.Length) {
                            formatter.Add(jstr.str[i].ToString());
                        }
                        else {
                            formatter.Add(" ");
                        }
                    }
                }
                //var shapeProduct = AType.ShapeProduct(Shape);
                //var chars = Enumerable.Range(0, (int)shapeProduct).Select(x=>GetChar(x));
                //return new Formatter(Shape,"").AddRange(chars).ToString();
                return formatter.ToString();
            }
            else if (typeof(T) == typeof(Byte) && Rank > 0 ) {
                var shape = ShapeCopy();
                var newShape = shape.Take(shape.Length - 1).ToArray();
                var shapeProduct = AType.ShapeProduct(newShape);
                var formatter = new Formatter(newShape);
                long offset = 0;
                long count = shape[shape.Length - 1];
                var ravel = (Byte[])(object)Ravel;
                for (var i = 0; i < shapeProduct; i++) {
                    formatter.Add(System.Text.Encoding.UTF8.GetString(ravel, (int)offset, (int)count));
                    offset += count;
                }
                return formatter.ToString();
            }
            else if (typeof(T) == typeof(Verb)) {
                var ravel = (Verb[])(object)Ravel;
                var str = new List<string>();
                foreach (var val in ravel) {                
                    str.Add(val.ToString());
                }
                return String.Join(" ", str);
            }
            if (Ravel.Length > 0)
                return new Formatter(Shape).AddRange(Ravel.Select(x => StringConverter(x))).ToString();
            else
                return "";
        }

        public T[] Copy(long count = 0, long skip = 0, bool ascending=true) {
            T[] z = new T[count];
            long yoffset = ascending ? skip : (Count - skip - count);
            for (long i = 0; i < count; i++) {
                z[i] = Ravel[yoffset+i];
            }          
            return z;
        }
    }

    public class AHelper {

        public static Dictionary<string, string> ToOptions(A<Box> xb) {
            var options = new Dictionary<string, string>();
            var ct = xb.Rank > 1 ? xb.Shape[xb.Shape.Length - 1] : 1;
            var offset = xb.Rank > 1 ? ct : 1;
            for (var i = 0; i < ct; i++) {
                options[xb.Ravel[i].val.ToString()] = xb.Ravel[(i + offset)].val.ToString();
            }
            return options;
        }
    }
    public class Parser {

        public static bool ThrowError = true;
        public static int OUTPUT_MAX_ROWS = 10000;

        public bool UseDecimal = false;
        public static int MAX_DECIMAL = -1;
        public bool SafeMode = false;
        public Verbs Verbs;
        public Adverbs Adverbs;
        public Conjunctions Conjunctions;

        public Func<string> ReadLine = null;
        
        public Dictionary<string, AType> Names;
        public Dictionary<string, AType> LocalNames;
        
        //needed for state in calldotnet procedures
        public Dictionary<string, object> Globals;

        public static Func<string, AType> NameResolver = null;
        public static Func<string[], string[]> ParseWordsHook = null;

        public static string COLUMN_ALIAS_KEYWORD = " is ";
        public static decimal DecimalInfinity = new decimal(1,0,0,false,27);

        char[] symbols = null;
        char[] symbolPrefixes = null;

        public Parser() {
            Names = new Dictionary<string, AType>();
            Globals = new Dictionary<string, object>();

            Verbs = new Verbs();
            Adverbs = new Adverbs(Verbs);
            Conjunctions = new Conjunctions(Verbs);
            Conjunctions.Parser = this;

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

            if (w.EndsWith(" : 0")) {
                w+= "'";
                while(true) {
                    var nextLine = ReadLine().Replace("'", "''");
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
            bool inDouble = false;
            for (var ci = 0; ci < w.Length; ci++ ) {
                var c = w[ci];
                if (!inQuote && c == '\'') { emit(); currentWord.Append(c); inQuote = true; }
                else if (inQuote && c == '\'' && (ci < w.Length-1 && w[ci+1] != '\'' && w[ci-1] != '\'')) { currentWord.Append(c); emit(); inQuote = !inQuote; }
                //else if (inQuote) { /*if (ci >= w.Length -1 || !(c == '\'' && w[ci-1] == '\'')) {*/ currentWord.Append(c); /*}*/  }
                else if (inQuote) { if (ci >= w.Length - 1 || !(c == '\'' && w[ci - 1] == '\'') || inDouble) { currentWord.Append(c); inDouble = false; } else { inDouble = true; } }
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
            var words = z.ToArray();
            if (ParseWordsHook != null) {
                words = ParseWordsHook(words);
            }
            return words;
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

        public AType exec(string cmd, Dictionary<string, AType> locals = null) {
            var prevNames = LocalNames;
            try {
                if (locals != null) { LocalNames = locals; }
                return parse(cmd);
            }
            catch  {
                throw;
            }
            finally {
                LocalNames = prevNames;
            }
            
        }

        public AType parse(string cmd) {
            const string MARKER = "`";
            cmd = MARKER + " " + cmd;

            Func<Token, bool> isEdge = token => token.word == MARKER || token.word == "=:" || token.word == "=."|| token.word == "(";
            Func<Token, bool> isVerb = token => ((token.val != null && token.val.GetType() == typeof(A<Verb>))); //|| (token.word != null && verbs.ContainsKey(token.word));
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
                else if (isEdgeOrNotConj(w1) && (isNoun(w2) || isVerb(w2)) && isConj(w3) && (isNoun(w2) || isVerb(w2))) { step = 4; } //conjunction
                else if (isEdgeOrNotConj(w1) && isVerb(w2) && isVerb(w3) && isVerb(w4) ) { step = 5; } //fork
                else if (isEdgeOrNotConj(w1) && isNoun(w2) && isVerb(w3) && isVerb(w4)) { step = 5; } //fork
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

                        var vop = op.val as A<Verb>;

                        if (vop != null) {
                            var verbs = ((A<Verb>)op.val);

                            //single verb argument
                            if (verbs.Count == 1) {
                                var z = new A<Verb>(0);
                                var v = ((A<Verb>)op.val).Ravel[0];
                                if (v.adverb != null) {
                                    v.childAdverb = v.adverb;
                                }
                                z.Ravel[0] = v;
                                z.Ravel[0].adverb = adv.word;
                                stack.Push(new Token { val = z });
                            }
                            else { //train
                                var z = new A<Verb>(verbs.Count + 1);
                                for (var k = 0; k < verbs.Count; k++) {
                                    z.Ravel[k + 1] = verbs.Ravel[k];
                                }
                                z.Ravel[0].adverb = adv.word;
                                stack.Push(new Token { val = z });
                            }
                        }
                        else {
                            var z = new A<Verb>(0);
                            z.Ravel[0] = new Verb { childNoun = op.val, adverb = adv.word };
                            stack.Push(new Token { val = z});
                        }
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
                        //special case of 'foo' & 'bar' to concatenate strings
                        else if (lhs.val is A<JString> && rhs.val is A<JString>) {
                            var z = Verbs.catenate(lhs.val as A<JString>, rhs.val as A<JString>);
                            stack.Push(new Token { val = z });
                        }
                        else {
                            var z = new A<Verb>(0);
                            if (isVerb(lhs)) {
                                //z.Ravel[0] = ((A<Verb>)lhs.val).Ravel[0];
                                var lv = ((A<Verb>)lhs.val);
                                if (lv.Count == 1)
                                    z.Ravel[0].childVerb = lv.Ravel[0];
                                else
                                    z.Ravel[0].childVerb = lv;
                            } else {
                                z.Ravel[0].op = lhs.word;
                            }
                            z.Ravel[0].conj = conj.word;
                            z.Ravel[0].rhs = rhs.word;
                            //todo figure out conjunctions taking verbs on right
                            if (rhs.val != null && rhs.word == null) {
                                z.Ravel[0].rhs = rhs.val.ToString();
                            }
                            stack.Push(new Token { val = z });
                        }
                        stack.Push(p1);
                    }
                    else if (step == 5) { //fork
                        var p1 = stack.Pop();
                        var v1 = stack.Pop();
                        var v2 = stack.Pop();
                        var v3 = stack.Pop();
                        var z = new A<Verb>(3);

                        if (!isNoun(v1)) {
                            z.Ravel[0] = ((A<Verb>)v1.val).Ravel[0];
                        }
                        else {
                            z.Ravel[0] = new Verb { rhs = v1.word };
                        }

                        var av2 = ((A<Verb>)v2.val);
                        if (av2.Count > 1) {
                            z.Ravel[1].childVerb = av2;
                        }
                        else {
                            z.Ravel[1] = av2.Ravel[0];
                        }
                        
                        
                        var av3 = ((A<Verb>)v3.val);
                        if (av3.Count > 1) {
                            z.Ravel[2].childVerb = av3;
                        }
                        else {
                            z.Ravel[2] = av3.Ravel[0];
                        }
                        
                        stack.Push(new Token { val = z });
                        stack.Push(p1);
                    }
                    else if (step == 7) { //copula
                        var name = stack.Pop();
                        var copula = stack.Pop();
                        var rhs = stack.Pop();

                        var names = name.word.Split(' ');
                        if (names.Length > 1 && rhs.val.GetCount() != names.Length) {
                            throw new ApplicationException("invalid length");
                        }
                        Dictionary<string, AType> nameRef = null;
                        if (copula.word == "=:" || LocalNames == null) {
                            nameRef = Names;
                        } else {
                            nameRef = LocalNames;
                        }
                        if (names.Length == 1) {
                            nameRef[name.word] = rhs.val;
                        } else {
                            for(var k = 0; k < names.Length; k++) {
                                var val = rhs.val.GetValA(k);
                                if (val.GetType() == typeof(A<Box>) && val.GetCount() == 1) {
                                    val = (val as A<Box>).First().val;
                                }
                                nameRef[names[k].Replace("'", "")] = val;
                            }
                        }
                            
                        
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
                        var val = AType.MakeA(newWord.word, Names, LocalNames);
                        var token = new Token();
                        if (val.GetType() == typeof(A<Undefined>)) {
                            token.word = newWord.word;
                            if (Verbs.expressionMap.ContainsKey(newWord.word)) {
                                token.val = new A<Verb>(0) { Ravel = new Verb[] { new Verb { op = newWord.word } } };
                            }
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
       
    //todo could use some cleanup... primarily interested in getting tests to pass for now
    public class Formatter {
        public string[][][] Table;
        public long[] shape;
        long tableIdx = 0;
        long rowIdx = 0;
        long columnIdx = 0;
        long columnLength = 0;
        long rowLength = 0;
        long tableLength = 0;
        long cellCount = 0;
        long totalSize = 0;
        string separator = " ";
        public Formatter(long[] shape, string separator = " ") {
            this.shape = shape;
            this.separator = separator;
            if (shape == null || shape.Length == 0) {
                tableLength = 1;
                rowLength = 1;
                columnLength = 1;
                this.shape = new long[] { 1 };
            }
            else if (shape.Length == 1) {
                tableLength = 1;
                rowLength = 1;
                columnLength = shape[0];
            }
            else if (shape.Length == 2) {
                tableLength = 1;
                columnLength = shape[shape.Length - 1];
                rowLength = shape[shape.Length - 2];
            }
            else if (shape.Length > 2) {
                tableLength = shape.Take(shape.Length - 2).Aggregate(1L, (x, y) => x * y);
                columnLength = shape[shape.Length - 1];
                rowLength = shape[shape.Length - 2];
            }
            totalSize = tableLength * columnLength * rowLength;
            Table = new string[tableLength][][];
            for (var t = 0L; t < tableLength; t++) {
                Table[t] = new string[rowLength][];
                for (var k = 0L; k < rowLength; k++) {
                    Table[t][k] = new string[columnLength];
                }
            }
        }

        public Formatter AddRange(IEnumerable<string> vals) {
            foreach (var val in vals) {
                if (cellCount >= totalSize) { return this; }
                Add(val);
            }
            return this;
        }
        public Formatter Add(string cell) {
            if (columnIdx >= columnLength) {
                rowIdx++;
                columnIdx = 0;
            }
            if (rowIdx >= rowLength) {
                rowIdx = 0;
                columnIdx = 0;
                tableIdx++;
            }
            Table[tableIdx][rowIdx][columnIdx++] = cell;
            cellCount++;
            return this;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            var rank = shape.Length;
            long[] odometer = new long[rank];

            
            var columnPadding = new long[columnLength];
            for (var t = 0L; t < tableLength ; t++) {
                for (var r = 0L; r < rowLength && r < Parser.OUTPUT_MAX_ROWS; r++) {
                    for (var c = 0L; c < columnLength; c++) {
                        if (Table[t][r][c] == null) continue;
                        var len = Table[t][r][c].Split('\n').Max(x => x.Length);
                        if (len > columnPadding[c]) {
                            columnPadding[c] = len;
                        }
                    }
                }
            }
            var spacer = separator;
            bool boxed = Table[0][0][0].StartsWith("+");
            for (var t = 0L; t < tableLength; t++) {
                string headerLine = "";
                for (var r = 0L; r < rowLength && r < Parser.OUTPUT_MAX_ROWS; r++) {
                    var lines = Table[t][r].Where(x=>x!=null).Select(x => x.Split('\n')).ToList();
                    var maxLines = lines.Max(x => x.Length);

                    //skip the footer line
                    if (boxed) { maxLines = maxLines - 1; }
                    for (var i = 0; i < maxLines; i++) {
                        for (var c = 0; c < columnLength; c++) {
                            if (c >= lines.Count) continue;

                            var line = i < lines[c].Length ? lines[c][i] : spacer;
                            if (boxed && line.Length > 0) {
                                line = line.Substring(1, line.Length - 1);
                                if (line.Length > 0) {
                                    line = line.Substring(0, line.Length - 1);
                                }

                                //footer row
                                if (i == lines[c].Length - 1) { line = ""; }
                                
                                line = line.PadRight((int)columnPadding[c] - 2, i == 0 ? '-' : ' ');
                                if (c == 0) {
                                    line = ((i == 0) ? "+" : "|") + line;
                                }
                                line = line + ((i == 0) ? "+" : "|");
                            }
                            else {
                                line = line.PadLeft((int)columnPadding[c]);
                            }
                            sb.Append(line);
                            if (i == 0 && r == 0 && boxed) { headerLine += line; }
                            if (!boxed) {
                                odometer[rank - 1]++;
                            }

                            if (c < columnLength - 1 && !boxed) {
                                sb.Append(spacer);
                            }
                            if (!boxed) {
                                for (var k = shape.Length - 1; k > 0; k--) {
                                    if (odometer[k] == shape[k]) {
                                        odometer[k] = 0;
                                        odometer[k - 1]++;
                                        sb.Append("\n");
                                    }
                                }
                            }

                        }
                        if (boxed) {
                            sb.Append("\n");
                        }
                    }
                }
                if (boxed) {
                    sb.Append(headerLine + "\n\n");
                    if (rank > 2) {
                        odometer[rank - 1] += columnLength;
                        odometer[rank - 2]++;
                        for (var k = shape.Length - 2; k > 0; k--) {
                            if (odometer[k] == shape[k]) {
                                odometer[k] = 0;
                                odometer[k - 1] = 0;
                                sb.Append("\n");
                            }
                        }
                    }
                }
            }
            var ret = sb.ToString();
            ret = ret.TrimEnd('\n');
            return ret;

        }
    }
}
