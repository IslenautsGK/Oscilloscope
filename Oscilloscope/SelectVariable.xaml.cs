using System.Windows;
using System.Windows.Controls;
using Oscilloscope.Models;
using Oscilloscope.ViewModels;

namespace Oscilloscope;

public partial class SelectVariable : UserControl
{
    internal SelectVariableViewModel ViewModel { get; }

    public SelectVariable()
    {
        ViewModel = new();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void TreeViewSelectedItemChanged(
        object? sender,
        RoutedPropertyChangedEventArgs<object> e
    )
    {
        if (e.NewValue is VariableTreeNode node)
            ViewModel.SelectedVariable = node;
    }
}
