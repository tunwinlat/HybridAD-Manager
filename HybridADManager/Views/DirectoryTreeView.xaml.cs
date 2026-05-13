using HybridADManager.Models;
using System.Windows;
using System.Windows.Controls;

namespace HybridADManager.Views;

public partial class DirectoryTreeView : UserControl
{
    public DirectoryTreeView()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is TreeView treeView && treeView.DataContext is ViewModels.DirectoryTreeViewModel vm)
        {
            vm.SelectedNode = e.NewValue as DirectoryNode;
        }
    }
}
