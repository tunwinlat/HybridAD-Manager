using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HybridADManager.Views.Dialogs;

public partial class FindDialog : Window
{
    public FindDialog()
    {
        InitializeComponent();
    }

    private void ResultsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem != null)
        {
            var viewModel = DataContext as ViewModels.FindDialogViewModel;
            viewModel?.OpenPropertiesCommand.Execute(null);
        }
    }
}
