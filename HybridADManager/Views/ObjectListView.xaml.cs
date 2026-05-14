using HybridADManager.Models;
using System.Windows;
using System.Windows.Controls;

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
}
