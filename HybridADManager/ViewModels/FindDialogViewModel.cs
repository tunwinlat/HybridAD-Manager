using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class FindDialogViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly string _domainName;

    [ObservableProperty]
    private string _searchName = string.Empty;

    [ObservableProperty]
    private string _searchEmail = string.Empty;

    [ObservableProperty]
    private string _searchDescription = string.Empty;

    [ObservableProperty]
    private string _searchPhone = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _results = new();

    [ObservableProperty]
    private DirectoryObject? _selectedResult;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private string _statusText = "Ready";

    public FindDialogViewModel(IActiveDirectoryService adService, string domainName)
    {
        _adService = adService;
        _domainName = domainName;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchName)
            && string.IsNullOrWhiteSpace(SearchEmail)
            && string.IsNullOrWhiteSpace(SearchDescription)
            && string.IsNullOrWhiteSpace(SearchPhone))
        {
            ErrorMessage = "Enter at least one search criterion.";
            return;
        }

        IsSearching = true;
        ErrorMessage = string.Empty;
        Results.Clear();

        try
        {
            var searchResults = await _adService.SearchObjectsAsync(
                _domainName,
                SearchName,
                SearchEmail,
                SearchDescription,
                SearchPhone);

            foreach (var result in searchResults)
            {
                Results.Add(result);
            }

            HasResults = Results.Count > 0;
            StatusText = $"{Results.Count} result(s) found";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = "Search failed";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void OpenProperties()
    {
        if (SelectedResult == null) return;

        if (SelectedResult is HybridUser user)
        {
            var propertySheet = new Views.PropertySheets.UserPropertySheet(user)
            {
                Owner = Application.Current.MainWindow
            };
            propertySheet.ShowDialog();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        SearchName = string.Empty;
        SearchEmail = string.Empty;
        SearchDescription = string.Empty;
        SearchPhone = string.Empty;
        Results.Clear();
        HasResults = false;
        ErrorMessage = string.Empty;
        StatusText = "Ready";
    }
}
