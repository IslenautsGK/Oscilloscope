using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Data;

namespace Oscilloscope;

public sealed partial class VariableColorPicker : UserControl
{
    private readonly string color;

    private readonly VariableColorPickerViewModel vm;

    public VariableColorPicker(string color)
    {
        this.color = color;
        vm = new() { Result = color };
        DataContext = vm;
        InitializeComponent();
    }

    private void ColorPickerCanceled(object sender, EventArgs e)
    {
        vm.Result = color;
        vm.CloseAction?.Invoke();
    }

    private void ColorPickerConfirmed(object? sender, FunctionEventArgs<Color> e)
    {
        vm.Result = e.Info.ToString();
        vm.CloseAction?.Invoke();
    }
}
