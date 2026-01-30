using System.Windows;
using System.Windows.Controls;
using Oscilloscope.Helpers;
using Oscilloscope.ViewModels;
using ScottPlot;
using ScottPlot.WPF;

namespace Oscilloscope.Helpers;

internal sealed class OscilloscopeMenu(WpfPlotBase plotBase, MainViewModel vm) : IPlotMenu
{
    public void ShowContextMenu(Pixel pixel)
    {
        var plot = plotBase.GetPlotAtPixel(pixel);
        if (plot is null)
            return;
        var menu = new ContextMenu();
        menu.Items.Add(new MenuItem { Header = "导出 Excel", Command = vm.SaveToExcelCommand });
        menu.Items.Add(new MenuItem { Header = "导入 Excel", Command = vm.LoadFromExcelCommand });
        menu.Items.Add(new Separator());
        var autoscale = new MenuItem { Header = "自动缩放" };
        autoscale.Click += Autoscale;
        menu.Items.Add(autoscale);
        menu.PlacementTarget = plotBase;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private void Autoscale(object? sender, RoutedEventArgs e)
    {
        plotBase.Plot.Axes.AutoScale();
        plotBase.Refresh();
    }

    public void Reset() { }

    public void Clear() { }

    public void Add(string Label, Action<Plot> action) { }

    public void AddSeparator() { }
}
