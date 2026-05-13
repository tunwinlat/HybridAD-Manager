using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using HybridADManager.Views.PropertySheets;
using System.Collections.ObjectModel;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class ObjectListViewModel : ObservableObject
{
    private readonly IActiveDirectoryService? _adService;
    private readonly IGraphService? _graphService;

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _objects = new();

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _filteredObjects = new();

    [ObservableProperty]
    private DirectoryObject? _selectedObject;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalCount;

    public ObjectListViewModel()
    {
        _adService = App.Current?.Services?.GetService(typeof(IActiveDirectoryService)) as IActiveDirectoryService;
        _graphService = App.Current?.Services?.GetService(typeof(IGraphService)) as IGraphService;
        _filteredObjects = _objects;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterObjects();
    }

    partial void OnObjectsChanged(ObservableCollection<DirectoryObject> value)
    {
        FilterObjects();
    }

    public async void LoadObjectsForNode(DirectoryNode node)
    {
        Objects.Clear();
        IsLoading = true;
        TotalCount = 0;

        try
        {
            if (node.DirectoryObject != null)
            {
                // Single object selected (leaf node in tree)
                Objects.Add(node.DirectoryObject);
            }
            else if (node.Children != null)
            {
                // Container node - try AD first, then fallback to tree children
                if (_adService != null && !string.IsNullOrEmpty(node.DistinguishedName))
                {
                    try
                    {
                        var adObjects = await _adService.GetObjectsInContainerAsync(node.DistinguishedName);
                        foreach (var obj in adObjects)
                        {
                            // Try to correlate with Entra if authenticated
                            if (_graphService != null && !string.IsNullOrEmpty(obj.Upn))
                            {
                                try
                                {
                                    var graphUser = await _graphService.GetUserByUpnAsync(obj.Upn);
                                    if (graphUser != null)
                                    {
                                        obj.EntraObjectId = graphUser.EntraObjectId;
                                        obj.ImmutableId = graphUser.ImmutableId;
                                        obj.SyncStatus.Status = graphUser.SyncStatus.Status;
                                    }
                                }
                                catch { /* ignore graph errors */ }
                            }
                            Objects.Add(obj);
                        }
                    }
                    catch
                    {
                        LoadFromTreeChildren(node);
                    }
                }
                else
                {
                    LoadFromTreeChildren(node);
                }
            }

            TotalCount = Objects.Count;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenProperties()
    {
        if (SelectedObject == null) return;

        Window? propertySheet = SelectedObject switch
        {
            HybridUser user => new UserPropertySheet(user),
            _ => null
        };

        if (propertySheet != null)
        {
            propertySheet.Owner = Application.Current.MainWindow;
            propertySheet.ShowDialog();
        }
    }

    [RelayCommand]
    private void RefreshList()
    {
        // TODO: Refresh from AD
    }

    private void LoadFromTreeChildren(DirectoryNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.DirectoryObject != null)
            {
                Objects.Add(child.DirectoryObject);
            }
        }
    }

    private void FilterObjects()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredObjects = Objects;
        }
        else
        {
            var filtered = new ObservableCollection<DirectoryObject>(
                Objects.Where(o =>
                    o.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            FilteredObjects = filtered;
        }
    }
}
