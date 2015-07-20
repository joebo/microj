NB. 0!:0 <'examples\worldbank.ijs'

countriesCode =: 0 : 0
//css_using MicroJ    
//css_using System.IO
//css_using System.Collections.Generic
//css_ref System.Web.Extensions
//css_ref Microsoft.CSharp.dll
//css_ref System.Collections.Generic
//css_using System.Threading

//string text = File.ReadAllText(@"c:\temp\countries.json");
new Thread(() => {
    Console.WriteLine("downloading from web async..");
    string text = new System.Net.WebClient().DownloadString("http://api.worldbank.org/countries/all?per_page=25000&format=json");
    Console.WriteLine("Complete downloading from web..");
    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
    ser.MaxJsonLength  = Int32.MaxValue;
    object[] ret = (object[])ser.DeserializeObject(text);

    Func<object[], string, Func<Dictionary<string, object>, string>, A<JString>> buildString = (arr, col, selector) => {
        var ct = arr.Length;
        var z = new MicroJ.A<JString>(ct);
        long offset = 0;
        var maxLen = 0;
        foreach(var row in arr) {
            var str = selector(row as Dictionary<string, object>);
            var len = str.Length;
            if (len > maxLen) {
                maxLen = len;
            }
            z.Ravel[offset++] = new JString { str = str };
        }
        z.Shape = new long[] {  ct, maxLen };
        return z;
    };
    var rows = ret[1] as object[];
    parser.Names["name_country_"] = buildString(rows, "name", x=>x["name"].ToString());

    //dynamic makes things nicer
    parser.Names["region_country_"] = buildString(rows, "name", x=>((dynamic)x["region"])["value"].ToString());
    parser.Names["incomeLevel_country_"] = buildString(rows, "name", x=>((dynamic)x["incomeLevel"])["value"].ToString());
    Console.WriteLine(parser.exec("regionTable region_country_").ToString());
    
}).Start();

return "";
)

getCountries  =: (150!:0) & countriesCode

getCountries''

regionTable =: ({.;#) /. ~ 

NB. this is what the output looks like
Note =: 0 : 0

regions=: ({.;#) /. ~ region_country_ )

+----------------------------------------------+--+ 
|Latin America & Caribbean (all income levels) |41| 
+----------------------------------------------+--+ 
|South Asia                                    |8 | 
+----------------------------------------------+--+ 
|Aggregates                                    |49| 
+----------------------------------------------+--+ 
|Sub-Saharan Africa (all income levels)        |48| 
+----------------------------------------------+--+ 
|Europe & Central Asia (all income levels)     |57| 
+----------------------------------------------+--+ 
|Middle East & North Africa (all income levels)|21| 
+----------------------------------------------+--+ 
|East Asia & Pacific (all income levels)       |37| 
+----------------------------------------------+--+ 
|North America                                 |3 | 
+----------------------------------------------+--+



 (\: ; 1&{"1 regions) { regions
+----------------------------------------------+--+
|Europe & Central Asia (all income levels)     |57|
+----------------------------------------------+--+
|Aggregates                                    |49|
+----------------------------------------------+--+
|Sub-Saharan Africa (all income levels)        |48|
+----------------------------------------------+--+
|Latin America & Caribbean (all income levels) |41|
+----------------------------------------------+--+
|East Asia & Pacific (all income levels)       |37|
+----------------------------------------------+--+
|Middle East & North Africa (all income levels)|21|
+----------------------------------------------+--+
|South Asia                                    |8 |
+----------------------------------------------+--+
|North America                                 |3 |
+----------------------------------------------+--+

)