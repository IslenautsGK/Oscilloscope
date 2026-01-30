using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Tools.Extension;

namespace Oscilloscope.ViewModels;

internal sealed partial class ConfirmViewModel : ObservableObject, IDialogResultable<bool>
{
    public bool Result { get; set; }

    public Action? CloseAction { get; set; }

    [ObservableProperty]
    public required partial string Title { get; set; }

    [ObservableProperty]
    public required partial string Message { get; set; }

    [RelayCommand]
    private void Ok()
    {
        Result = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = false;
        CloseAction?.Invoke();
    }
}
