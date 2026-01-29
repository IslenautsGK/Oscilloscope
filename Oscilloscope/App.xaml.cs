using System.Windows;
using System.Windows.Threading;
using HandyControl.Controls;

namespace Oscilloscope;

public partial class App : Application
{
    private void ApplicationDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    )
    {
        var show = new ShowException(e.Exception);
        var dialog = Dialog.Show(show, "DefaultDialogContainer");
        show.CloseAction = dialog.Close;
        e.Handled = true;
    }
}
