using System.Windows;

namespace Oscilloscope;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddAsync();
        async Task AddAsync()
        {
            await Task.Delay(1000);
            //var streamer = plot.Plot.Add.DataStreamerXY(1000);
            //streamer.ManageAxisLimits = false;
            //streamer.ViewScrollLeft();
            var streamer = new OscilloscopeStreamer();
            plot.Plot.PlottableList.Add(streamer);
            var i = 0;
            streamer.ManageAxisLimits = true;
            while (i < 1000)
            {
                await Task.Delay(10);
                streamer.Add(Math.Sin(i / 20.0 * Math.PI));
                plot.Refresh();
                i++;
            }
            streamer.ManageAxisLimits = false;
        }
    }
}
