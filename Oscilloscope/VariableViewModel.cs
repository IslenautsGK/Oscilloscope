using CommunityToolkit.Mvvm.ComponentModel;

namespace Oscilloscope;

internal sealed partial class VariableViewModel : ObservableObject
{
    [ObservableProperty]
    public partial VariableInfo Variable { get; set; }

    [ObservableProperty]
    public partial string Color { get; set; } = "#000000";
}
