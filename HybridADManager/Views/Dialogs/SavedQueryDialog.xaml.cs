using HybridADManager.Models;
using HybridADManager.ViewModels;
using System.Windows;

namespace HybridADManager.Views.Dialogs;

public partial class SavedQueryDialog : Window
{
    public SavedQueryDialog()
    {
        InitializeComponent();
        var vm = new SavedQueryDialogViewModel();
        vm.RequestClose += (sender, result) =>
        {
            DialogResult = result;
            Close();
        };
        DataContext = vm;
    }

    public SavedQuery? SavedQuery => (DataContext as SavedQueryDialogViewModel)?.ToSavedQuery();
}
