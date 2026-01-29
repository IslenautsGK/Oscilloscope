using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
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
        IRecipient<OscilloscopeMessage>,
        IRecipient<RequestDataMessage>,
        IRecipient<LoadDataMessage>
{
    private readonly MainViewModel vm;

    private bool saved;

    private bool clear = true;

    private readonly List<OscilloscopeStreamer> streamers = [];

    private DateTime nextRefreshTime = DateTime.Now;

    public MainWindow()
    {
        vm = new();
        DataContext = vm;
        InitializeComponent();
        plot.Menu = new OscilloscopeMenu(plot, vm);
        vm.ReceiveData += ReceiveData;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (saved)
            return;
        var task = vm.SaveAppStatusAsync();
        if (task.IsCompleted)
            return;
        e.Cancel = true;
        await task;
        saved = true;
        Close();
    }

    private void PlotMouseEnter(object? sender, MouseEventArgs e) =>
        variablePanel.Visibility = Visibility.Visible;

    private void PlotMouseLeave(object? sender, MouseEventArgs e) =>
        variablePanel.Visibility = Visibility.Collapsed;

    private void PlotMouseMove(object? sender, MouseEventArgs e)
    {
        var index = (int)
            Math.Round(plot.Plot.GetCoordinates(plot.GetPlotPixelPosition(e)).X * 1000 / vm.Cycle);
        vm.CurTime = index * vm.Cycle / 1000.0;
        foreach (var streamer in streamers)
            streamer.UpdateValueByIndex(index);
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
        message.Reply(
            Dialog
                .Show(new VariableColorPicker(message.Color), "DefaultDialogContainer")
                .GetResultAsync<string>()
        );

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
                    CreateStreamer();
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

    private void CreateStreamer()
    {
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
                LineColor = uint.TryParse(color, NumberStyles.HexNumber, null, out var argb)
                    ? Color.FromARGB(argb)
                    : Colors.Black,
                Cycle = vm.Cycle,
                UpdateCurValue = v => variable.CurValue = v,
            };
            streamers.Add(streamer);
            plot.Plot.PlottableList.Add(streamer);
        }
        plot.Plot.HideLegend();
    }

    void IRecipient<RequestDataMessage>.Receive(RequestDataMessage message) =>
        message.Reply(GetData());

    private IEnumerable<Dictionary<string, double>> GetData()
    {
        var index = 0;
        while (true)
        {
            var hasData = false;
            var dictionary = new Dictionary<string, double>
            {
                ["时间"] = index * vm.Cycle / 1000.0,
            };
            foreach (var streamer in streamers)
            {
                if (streamer.FillData(dictionary, index))
                    hasData = true;
            }
            index++;
            if (!hasData)
                yield break;
            yield return dictionary;
        }
    }

    void IRecipient<LoadDataMessage>.Receive(LoadDataMessage message)
    {
        CreateStreamer();
        foreach (var data in message.Datas.OfType<IDictionary<string, object?>>())
        {
            foreach (var streamer in streamers)
            {
                if (data.TryGetValue(streamer.LegendText, out var value) && value is double d)
                    streamer.Add(d);
            }
        }
        foreach (var streamer in streamers)
            streamer.ManageAxisLimits = false;
        plot.Plot.Axes.AutoScale();
        plot.Refresh();
    }
}
