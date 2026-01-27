using HandyControl.Tools.Extension;

namespace Oscilloscope;

internal sealed partial class VariableColorPickerViewModel : IDialogResultable<string>
{
    public required string Result { get; set; }

    public Action? CloseAction { get; set; }
}
