using System.Windows.Controls;
using Oscilloscope.ViewModels;

namespace Oscilloscope;

public partial class Confirm : UserControl
{
    public Confirm(string title, string message)
    {
        DataContext = new ConfirmViewModel() { Title = title, Message = message };
        InitializeComponent();
    }
}
