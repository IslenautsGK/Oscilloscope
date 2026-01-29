using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Oscilloscope;

internal sealed partial class VariableViewModel : ObservableObject
{
    [ObservableProperty]
    public partial VariableInfo Variable { get; set; }

    [ObservableProperty]
    public required partial string Color { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial double CurValue { get; set; } = double.NaN;
}
