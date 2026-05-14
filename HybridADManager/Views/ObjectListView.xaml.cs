using HybridADManager.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HybridADManager.Views;

public partial class ObjectListView : UserControl
{
    public ObjectListView()
    {
        InitializeComponent();
    }

    private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.DataContext is ViewModels.ObjectListViewModel vm)
        {
            vm.OpenPropertiesCommand.Execute(null);
        }
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ViewModels.ObjectListViewModel vm)
        {
            vm.SelectedObjects.Clear();
            foreach (var item in ListView.SelectedItems)
            {
                if (item is DirectoryObject obj)
                    vm.SelectedObjects.Add(obj);
            }
            vm.SelectedCount = ListView.SelectedItems.Count;
        }
    }

    private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is ListView listView)
        {
            var item = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (item != null && item.DataContext is Models.DirectoryObject obj)
            {
                DragDrop.DoDragDrop(listView, obj, DragDropEffects.Move);
            }
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
