using System.Windows;
using System.Windows.Controls;

namespace Oscilloscope;

public sealed partial class ShowException : UserControl
{
    public string Message { get; }

    public string? StackTrace { get; }

    public Action? CloseAction { private get; set; }

    public ShowException(Exception exception)
    {
        Message = exception.Message;
        StackTrace = exception.StackTrace;
        InitializeComponent();
    }

    private void ButtonClick(object? sender, RoutedEventArgs e) => CloseAction?.Invoke();
}
