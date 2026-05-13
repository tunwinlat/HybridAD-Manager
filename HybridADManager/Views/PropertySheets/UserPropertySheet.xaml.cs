using HybridADManager.Models;
using HybridADManager.ViewModels;
using System.Windows;

namespace HybridADManager.Views.PropertySheets;

public partial class UserPropertySheet : Window
{
    public UserPropertySheet(HybridUser user)
    {
        InitializeComponent();
        DataContext = new UserPropertySheetViewModel(user);
    }
}
