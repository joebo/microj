A=: i. 10000
B=: i. 10000

serverCode =: 0 : 0
//css_using MicroJ    
//css_using System.IO
//css_using System.Collections.Generic
//css_using System.Linq
//css_using System.Text
//css_using System.Threading.Tasks
//css_using System.Net
//css_using System.Threading
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
                     newParser.SafeMode = true;
                     if (input != null) {
                         foreach(var key in input) {
                             var parts = key.Key.Split(':');
                             var xpos = Convert.ToInt32(parts[0]);
                             var ypos = Convert.ToInt32(parts[1]);
                             var a = newParser.exec(key.Value.ToString());
                             Console.WriteLine(a.ToString());
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
                                     arr[(xpos+k).ToString()+':'+(ypos+i).ToString()] = a.GetString(offset);
                                     offset++;
                                 }
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
