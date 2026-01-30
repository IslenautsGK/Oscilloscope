using HandyControl.Tools.Extension;

namespace Oscilloscope.ViewModels;

internal sealed class VariableColorPickerViewModel : IDialogResultable<string>
{
    public required string Result { get; set; }

    public Action? CloseAction { get; set; }
}
