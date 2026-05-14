using HybridADManager.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

    private void TreeView_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Models.DirectoryObject)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void TreeView_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(Models.DirectoryObject))) return;

        var droppedObject = e.Data.GetData(typeof(Models.DirectoryObject)) as Models.DirectoryObject;
        if (droppedObject == null) return;

        // Find the target node from the drop position
        var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
        if (treeViewItem == null) return;

        var targetNode = treeViewItem.DataContext as Models.DirectoryNode;
        if (targetNode == null) return;

        // Only allow dropping onto container nodes (OUs, Builtin, Users, etc.)
        var validTypes = new[] { 
            Models.NodeType.Domain, Models.NodeType.BuiltinContainer, 
            Models.NodeType.ComputersContainer, Models.NodeType.DomainControllersContainer,
            Models.NodeType.UsersContainer, Models.NodeType.CustomOU 
        };
        if (!validTypes.Contains(targetNode.NodeType))
        {
            MessageBox.Show("Can only drop onto organizational units or containers.", "Invalid Target", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Ask for confirmation
        var result = MessageBox.Show(
            $"Move '{droppedObject.DisplayName}' to '{targetNode.DisplayName}'?",
            "Confirm Move", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Perform the move via VM command
        if (DataContext is ViewModels.DirectoryTreeViewModel vm)
        {
            await vm.MoveObjectAsync(droppedObject, targetNode);
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor) return ancestor;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
