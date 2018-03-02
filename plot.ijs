NB. plot i. 100 2

plotCode=: 0 : 0
//css_using System.Windows
//css_using System.Collections.Generic
//css_using System.Windows.Forms
//css_using OxyPlot
//css_using System.Threading
//css_using MicroJ
//css_ref System.Runtime, System.Windows.Forms, System.Drawing, System.Collections
//css_ref OxyPlot, OxyPlot.WindowsForms


/*
    UNCOMMENT for wpf
   //css_using OxyPlot.WindowsForms;
   //css_ref WindowsBase, System.Xaml, WindowBase, PresentationCore, PresentationFramework
*/

var model = new PlotModel();
//model.Title = "LineSeries with default style";
var lineSeries1 = new OxyPlot.Series.LineSeries();
lineSeries1.Title = "LineSeries 1";

for(var i = 0; i < v.GetCount(); i++ ) {
if (v.Shape.Rank == 2) {
lineSeries1.Points.Add(new DataPoint(Double.Parse(v.GetString(i)),Double.Parse(v.GetString(i+1))));
}
else {
lineSeries1.Points.Add(new DataPoint(i,Double.Parse(v.GetString(i+1))));
}
i+=1;
}
model.Series.Add(lineSeries1);


/*
//for WPF, but blocks the thread
var w = new Window() { Title = "OxyPlot.Wpf.Plot : " + model.Title, Width = 500, Height = 500 };
var chart = new OxyPlot.Wpf.PlotView();
chart.Model = model;
w.Content = chart;
new System.Windows.Application().Run(w);
*/

var chart = new OxyPlot.WindowsForms.PlotView();
var w  = new Form();
w.BackColor = System.Drawing.Color.White;
w.Size = new System.Drawing.Size(600,600);
chart.Dock = DockStyle.Fill;
w.Controls.Add(chart);
chart.Model = model;
new Thread(() => System.Windows.Forms.Application.Run(w)).Start();


return new A<long>(0);
)
plot =: (150!:0) & plotCode