using System.Windows;
using System.Windows.Threading;

namespace Oscilloscope;

public partial class App : Application
{
    private void ApplicationDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e
    ) => MessageBox.Show($"{e.Exception.Message}\n{e.Exception.StackTrace}");
}
