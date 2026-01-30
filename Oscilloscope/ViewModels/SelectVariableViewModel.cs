using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Tools.Extension;
using Oscilloscope.Models;

namespace Oscilloscope.ViewModels;

internal sealed partial class SelectVariableViewModel
    : ObservableObject,
        IDialogResultable<SelectVariableResult?>
{
    public SelectVariableResult? Result { get; set; }

    public Action? CloseAction { get; set; }

    [ObservableProperty]
    public partial List<VariableTreeNode>? VariableTree { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial VariableTreeNode? SelectedVariable { get; set; }

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [RelayCommand(CanExecute = nameof(OkCanExecute))]
    private void Ok()
    {
        if (SelectedVariable is not { } variable)
            return;
        var displayName = DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = variable.Variable.Name;
        Result = new(variable.Variable, displayName);
        CloseAction?.Invoke();
    }

    private bool OkCanExecute() => SelectedVariable is not null;

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseAction?.Invoke();
    }
}
