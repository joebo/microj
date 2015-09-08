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
    ser.MaxJsonLength = int.MaxValue;
    
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

                 var MAX_COL = 100;
                 var PAGE_SIZE = 10000;
                 var totalRowCount = 0;
                 var rowCount = 0;
                 
                 if (file.StartsWith(pathSep)) { file = "." + file; }
                 if (file.Contains("..")) { file = ""; }
                 if (System.IO.File.Exists(file)) {
                     rstr = File.ReadAllText(file);
                 }
                 else {
                     var newParser = new Parser();
                     //copy names from parent
                     foreach(var key in parser.Names.Keys) {
                         newParser.Names[key] = parser.Names[key];
                     }
                     newParser.SafeMode = true;
                     string json="";
                     using(var sr = new StreamReader(Request.InputStream)) {
                         json = sr.ReadToEnd();
                     }
                     var input = ser.DeserializeObject(json) as object[];

                     var sb = new System.Text.StringBuilder();
                     if (input != null) {
                         foreach(var line in input) {
                             if (line == "") continue;
                             
                             sb.AppendLine("    " + line);
                             try {
                                 var a = newParser.exec(line.ToString());
                                 sb.AppendLine(a.ToString());
                             }
                             catch (Exception e)
                             {
                                 sb.AppendLine(e.ToString());
                             }
                             //Console.WriteLine(line.ToString());
                         }
                         rstr = sb.ToString();
                     }
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

return new A<long>(0);

)

startServer  =: (150!:0) & serverCode

startServer''


