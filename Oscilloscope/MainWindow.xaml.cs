using System.Globalization;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Tools.Extension;
using Microsoft.Win32;
using ScottPlot;
using Dialog = HandyControl.Controls.Dialog;

namespace Oscilloscope;

public sealed partial class MainWindow
    : Window,
        IRecipient<SaveFileMessage>,
        IRecipient<OpenFileMessage>,
        IRecipient<VariableColorMessage>,
        IRecipient<OscilloscopeMessage>
{
    private readonly MainViewModel vm;

    private bool clear = true;

    private readonly List<OscilloscopeStreamer> streamers = [];

    private DateTime nextRefreshTime = DateTime.Now;

    public MainWindow()
    {
        vm = new();
        DataContext = vm;
        InitializeComponent();
        vm.ReceiveData += ReceiveData;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void PlotMouseEnter(object sender, MouseEventArgs e) =>
        valuePanel.Visibility = Visibility.Visible;

    private void PlotMouseLeave(object? sender, MouseEventArgs e) =>
        valuePanel.Visibility = Visibility.Collapsed;

    private void PlotMouseMove(object? sender, MouseEventArgs e)
    {
        var c = plot.Plot.GetCoordinates(plot.GetPlotPixelPosition(e));
        valuePanel.Text = c.ToString();
    }

    private void ReceiveData(double[] data)
    {
        if (data.Length != streamers.Count)
            return;
        for (var i = 0; i < data.Length; i++)
            streamers[i].Add(data[i]);
        var now = DateTime.Now;
        if (now > nextRefreshTime)
        {
            plot.Refresh();
            nextRefreshTime = now.AddMilliseconds(15);
        }
    }

    void IRecipient<SaveFileMessage>.Receive(SaveFileMessage message)
    {
        var dialog = new SaveFileDialog()
        {
            Title = message.Title,
            FileName = message.FileName,
            Filter = message.Filter,
        };
        if (dialog.ShowDialog() == true)
            message.Reply(dialog.FileName);
        else
            message.Reply(null);
    }

    void IRecipient<OpenFileMessage>.Receive(OpenFileMessage message)
    {
        var dialog = new OpenFileDialog() { Title = message.Title, Filter = message.Filter };
        if (dialog.ShowDialog() == true)
            message.Reply(dialog.FileName);
        else
            message.Reply(null);
    }

    void IRecipient<VariableColorMessage>.Receive(VariableColorMessage message) =>
        message.Reply(Dialog.Show(new VariableColorPicker(message.Color)).GetResultAsync<string>());

    void IRecipient<OscilloscopeMessage>.Receive(OscilloscopeMessage message)
    {
        switch (message.Status)
        {
            case OscilloscopeStatus.Stop:
                clear = true;
                foreach (var streamer in streamers)
                    streamer.ManageAxisLimits = false;
                break;
            case OscilloscopeStatus.Play:
                if (clear)
                {
                    clear = false;
                    streamers.Clear();
                    plot.Plot.Clear();
                    foreach (var variable in vm.Variables)
                    {
                        var color = variable.Color;
                        if (color.StartsWith('#'))
                            color = color[1..];
                        if (color.Length == 6)
                            color = "FF" + color;
                        var streamer = new OscilloscopeStreamer
                        {
                            LegendText = variable.Variable.Name ?? "",
                            LineColor = uint.TryParse(
                                color,
                                NumberStyles.HexNumber,
                                null,
                                out var argb
                            )
                                ? Color.FromARGB(argb)
                                : Colors.Black,
                            Cycle = vm.Cycle,
                        };
                        streamers.Add(streamer);
                        plot.Plot.PlottableList.Add(streamer);
                    }
                }
                else
                {
                    foreach (var streamer in streamers)
                        streamer.ManageAxisLimits = true;
                }
                break;
            case OscilloscopeStatus.Pause:
                foreach (var streamer in streamers)
                    streamer.ManageAxisLimits = false;
                break;
            default:
                break;
        }
    }
}
