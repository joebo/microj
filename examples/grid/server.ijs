serverCode =: 0 : 0
//css_using MicroJ    
//css_using System.IO
//css_using System.Collections.Generic
//css_using System.Linq
//css_using System.Text
//css_using System.Threading.Tasks
//css_using System.Net
//css_using System.Threading
//css_using System.Text.RegularExpressions
//css_ref System.Web.Extensions

ThreadStart proc = () => {
    HttpListener listener = new HttpListener();
    parser.Globals["HttpListener"] = listener;
    
    listener.Prefixes.Add("http://localhost:8080/");

    listener.Start();
    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
    
    Console.WriteLine("Webserver running...");
    while (listener.IsListening)
    {
        ThreadPool.QueueUserWorkItem((c) => {
             var ctx = c as HttpListenerContext;
             try
             {
                 string rstr = "hello world"; 
                 var Request = ctx.Request;
                 var url = Request.RawUrl;
                 var pathSep = new string(new char[] { Path.DirectorySeparatorChar });
                 string file = url.Replace("/", pathSep);
                 rstr = file;
                 if (file.StartsWith(pathSep)) { file = "." + file; }
                 if (file.Contains("..")) { file = ""; }
                 if (System.IO.File.Exists(file)) {
                     rstr = File.ReadAllText(file);
                 }
                 else {
                     string json="";
                     using(var sr = new StreamReader(Request.InputStream)) {
                         json = sr.ReadToEnd();
                     }
                     var arr = new Dictionary<string, string>();
                     var input = ser.DeserializeObject(json) as Dictionary<string, object>;
                     var newParser = new Parser();

                     //copy names from parent
                     foreach(var key in parser.Names.Keys) {
                         newParser.Names[key] = parser.Names[key];
                     }
                     newParser.SafeMode = true;
                     if (input != null) {
                         foreach(var key in input) {
                             var parts = key.Key.Split(':');
                             var xpos = Convert.ToInt32(parts[0]);
                             var ypos = Convert.ToInt32(parts[1]);

                             var cmd = key.Value.ToString();
                             
                             try {
                                 var anonIdx = 0;
                                 var offsetRx = new Regex("OFFSET\\((\\d+),(\\d+),(\\d+),(\\d+)\\)");
                                 cmd = offsetRx.Replace(cmd, new MatchEvaluator(match => {
                                     var shape = new long[] { Convert.ToInt32(match.Groups[4].Value)+1, Convert.ToInt32(match.Groups[3].Value) + 1};
                                     var xoffset = Convert.ToInt32(match.Groups[1].Value);
                                     var yoffset = Convert.ToInt32(match.Groups[2].Value);
                                     string testVal = "";
                                     double testNum;
                                     bool isNum = false;
                                     if (arr.TryGetValue((xoffset).ToString() + ":" + (yoffset).ToString(), out testVal)) {
                                         if (Double.TryParse(testVal, out testNum)) {
                                             isNum = true;
                                         }
                                     }
                                     if (isNum) {
                                         var anon = new A<double>(shape);
                                         var anonOffset = 0;
                                         for(var yi = 0; yi < shape[0]; yi++) {
                                             for(var xi = 0; xi < shape[1]; xi++) {
                                                 string val = "";
                                                 if (arr.TryGetValue((xoffset+xi).ToString() + ":" + (yoffset+yi).ToString(), out val)) {
                                                     anon.Ravel[anonOffset++] = Convert.ToDouble(val);
                                                 } else {
                                                     anon.Ravel[anonOffset++] = 0;
                                                 }
                                                 
                                             }
                                         }
                                         var newName = "anon" + anonIdx.ToString();
                                         newParser.Names[newName] = anon;
                                         return newName;
                                     }
                                     else {
                                         var anon = new A<Box>(shape);
                                         var anonOffset = 0;
                                         for(var yi = 0; yi < shape[0]; yi++) {
                                             for(var xi = 0; xi < shape[1]; xi++) {
                                                 string val = "";
                                                 
                                                 if (arr.TryGetValue((xoffset+xi).ToString() + ":" + (yoffset+yi).ToString(), out val)) {

                                                 }
                                                 anon.Ravel[anonOffset++] = new Box { val = new A<JString>(0) { Ravel = new JString[] { new JString { str = val } } } };
                                             }
                                         }
                                         var newName = "anon" + anonIdx.ToString();
                                         newParser.Names[newName] = anon;
                                         return newName;
                                     }
                                     return match.ToString();
                                 }));
                                 Console.WriteLine(String.Format("{0}:{1} {2}", DateTime.Now, Request.RemoteEndPoint, cmd));
                                 var a = newParser.exec(cmd);
                                 
                                 //Console.WriteLine(a.ToString());
                                 var ct = a.GetCount();
                                 var cols = a.Shape == null ? 1 : a.Shape[a.Shape.Length-1];

                                 //hack for string
                                 if (a.Type == typeof(JString)) {
                                     ct = a.Rank > 1 ? a.Shape[0] : 1;
                                     cols = 1;
                                 }
                                 var offset = 0;
                                 for(var i = 0; i < ct/cols; i++) {
                                     for(var k = 0; k < cols; k++) {
                                         var val = "";
                                         if (a.Type != typeof(Box)) {
                                             val = a.GetString(offset);
                                             if (a.Type == typeof(JString)) {
                                                 //val = "'" + val + "'";
                                             }
                                         }
                                         else {
                                             val = ((A<Box>)a).Ravel[offset].val.GetString(0);
                                         }
                                         arr[(xpos+k).ToString()+':'+(ypos+i).ToString()] = val;
                                         offset++;
                                     }
                                 }
                             }
                             catch(Exception e) {
                                 arr[(xpos).ToString()+':'+(ypos).ToString()] = e.ToString().Substring(0,20);
                             }
                         }
                     }
                     rstr = ser.Serialize(arr);
                 }
                 byte[] buf = Encoding.UTF8.GetBytes(rstr);
                 ctx.Response.ContentLength64 = buf.Length;
                 ctx.Response.OutputStream.Write(buf, 0, buf.Length);
             }
            catch (Exception e) { Console.WriteLine(e); }
             finally
             {
                 // always close the stream
                 ctx.Response.OutputStream.Close();
             }
         }, listener.GetContext());
    }
    listener.Start();
};

if (parser.Globals.ContainsKey("HttpListener")) {
    var oldListener = parser.Globals["HttpListener"] as HttpListener;
    oldListener.Stop();
}
if (parser.Globals.ContainsKey("HttpListenerThread")) {
    var oldThread = parser.Globals["HttpListenerThread"] as Thread;
    oldThread.Abort();
}

var thread = new Thread(proc);
thread.Start();

parser.Globals["HttpListenerThread"] = thread;

return "";

)

startServer  =: (150!:0) & serverCode

startServer''


