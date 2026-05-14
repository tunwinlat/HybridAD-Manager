using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;

namespace HybridADManager.ViewModels;

public partial class SavedQueryDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _filterName = string.Empty;

    [ObservableProperty]
    private string _filterEmail = string.Empty;

    [ObservableProperty]
    private string _filterDescription = string.Empty;

    [ObservableProperty]
    private string _filterPhone = string.Empty;

    public event EventHandler<bool>? RequestClose;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return;

        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    public SavedQuery ToSavedQuery()
    {
        return new SavedQuery
        {
            Name = Name,
            Description = Description,
            FilterName = string.IsNullOrWhiteSpace(FilterName) ? null : FilterName,
            FilterEmail = string.IsNullOrWhiteSpace(FilterEmail) ? null : FilterEmail,
            FilterDescription = string.IsNullOrWhiteSpace(FilterDescription) ? null : FilterDescription,
            FilterPhone = string.IsNullOrWhiteSpace(FilterPhone) ? null : FilterPhone
        };
    }
}
