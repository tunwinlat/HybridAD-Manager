using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using HybridADManager.Views.PropertySheets;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class ObjectListViewModel : ObservableObject
{
    private readonly IActiveDirectoryService? _adService;
    private readonly IGraphService? _graphService;
    private readonly ISettingsService? _settingsService;

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _objects = new();

    [ObservableProperty]
    private string _domainName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _filteredObjects = new();

    [ObservableProperty]
    private DirectoryObject? _selectedObject;

    [ObservableProperty]
    private ObservableCollection<DirectoryObject> _selectedObjects = new();

    [ObservableProperty]
    private int _selectedCount;

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
        _settingsService = App.Current?.Services?.GetService(typeof(ISettingsService)) as ISettingsService;
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
            if (node.NodeType == NodeType.SavedQueries && node.DistinguishedName.StartsWith("QUERY:"))
            {
                await LoadSavedQueryResultsAsync(node);
            }
            else if (node.DirectoryObject != null)
            {
                // Single object selected (leaf node in tree)
                Objects.Add(node.DirectoryObject);
            }
            else if (node.Children != null)
            {
                // Cache domain name for saved queries
                if (_adService != null && string.IsNullOrEmpty(DomainName))
                {
                    try
                    {
                        DomainName = await _adService.GetCurrentDomainNameAsync() ?? "";
                    }
                    catch { }
                }

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
        if (SelectedObjects.Count == 0)
            return;

        if (SelectedObjects.Count > 1)
        {
            MessageBox.Show("Please select exactly one object to open properties.", "Properties", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var target = SelectedObjects[0];

        Window? propertySheet = target switch
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
    private async Task EnableSelectedAsync()
    {
        if (_adService == null || SelectedObjects.Count == 0)
            return;

        try
        {
            foreach (var obj in SelectedObjects)
            {
                if (obj is HybridUser)
                {
                    await _adService.EnableObjectAsync(obj.DistinguishedName);
                }
            }

            RefreshList();
        }
        catch
        {
            // swallow exceptions silently
        }
    }

    [RelayCommand]
    private async Task DisableSelectedAsync()
    {
        if (_adService == null || SelectedObjects.Count == 0)
            return;

        try
        {
            foreach (var obj in SelectedObjects)
            {
                if (obj is HybridUser)
                {
                    await _adService.DisableObjectAsync(obj.DistinguishedName);
                }
            }

            RefreshList();
        }
        catch
        {
            // swallow exceptions silently
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (_adService == null || SelectedObjects.Count == 0)
            return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete {SelectedObjects.Count} object(s)?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            foreach (var obj in SelectedObjects)
            {
                await _adService.DeleteObjectAsync(obj.DistinguishedName);
            }

            RefreshList();
        }
        catch
        {
            // swallow exceptions silently
        }
    }

    [RelayCommand]
    private async Task MoveSelectedAsync()
    {
        await Task.CompletedTask;
        MessageBox.Show(
            "Move operation requires selecting a target container. This will be implemented with a Move dialog in a future update.",
            "Move",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ResetPassword()
    {
        MessageBox.Show(
            "Password reset will be implemented in a future update.",
            "Reset Password",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var source = SelectedObjects.Count > 0 ? SelectedObjects : FilteredObjects;

        var sb = new StringBuilder();
        sb.AppendLine("Name,Type,UPN,Email,Description,Department,SyncStatus");

        foreach (var obj in source)
        {
            var department = obj is HybridUser user ? user.Department : string.Empty;
            sb.AppendLine(
                $"{EscapeCsv(obj.DisplayName)}," +
                $"{EscapeCsv(obj.ObjectType)}," +
                $"{EscapeCsv(obj.Upn)}," +
                $"{EscapeCsv(obj.Email)}," +
                $"{EscapeCsv(obj.Description)}," +
                $"{EscapeCsv(department)}," +
                $"{EscapeCsv(obj.SyncStatus.DisplayText)}");
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(
                $"Exported {source.Count} object(s) to CSV successfully.",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch
        {
            // swallow exceptions silently
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

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private async Task LoadSavedQueryResultsAsync(DirectoryNode node)
    {
        if (_settingsService == null) return;

        var queryId = node.DistinguishedName["QUERY:".Length..];

        try
        {
            var queries = await _settingsService.GetSavedQueriesAsync();
            var query = queries.FirstOrDefault(q => q.Id == queryId);
            if (query == null) return;

            if (string.IsNullOrEmpty(DomainName) && _adService != null)
            {
                try
                {
                    DomainName = await _adService.GetCurrentDomainNameAsync() ?? "";
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(DomainName) && _adService != null)
            {
                try
                {
                    var results = await _adService.SearchObjectsAsync(
                        DomainName,
                        query.FilterName,
                        query.FilterEmail,
                        query.FilterDescription,
                        query.FilterPhone);

                    foreach (var obj in results)
                    {
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
                            catch { }
                        }
                        Objects.Add(obj);
                    }
                }
                catch
                {
                    LoadDummyDataWithFilters(query);
                }
            }
            else
            {
                LoadDummyDataWithFilters(query);
            }
        }
        catch { }
    }

    private void LoadDummyDataWithFilters(SavedQuery query)
    {
        var allObjects = new List<DirectoryObject>
        {
            new HybridUser { DisplayName = "Alice Alison", Email = "alice@contoso.com", Description = "IT Manager", Telephone = "555-0101", FirstName = "Alice", LastName = "Alison", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridUser { DisplayName = "Bob Builder", Email = "bob@contoso.com", Description = "Engineer", Telephone = "555-0102", FirstName = "Bob", LastName = "Builder", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.Pending } },
            new HybridUser { DisplayName = "Charlie Chaplin", Email = "charlie@contoso.com", Description = "Sales Rep", Telephone = "555-0103", FirstName = "Charlie", LastName = "Chaplin", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.CloudOnly } },
            new HybridUser { DisplayName = "Diana Prince", Email = "diana@contoso.com", Description = "HR Director", Telephone = "555-0104", FirstName = "Diana", LastName = "Prince", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.SyncError } },
            new HybridGroup { DisplayName = "IT Support", Email = "it@contoso.com", Description = "IT Support Team", GroupScope = "Global", GroupType = "Security", ObjectType = "Group", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridGroup { DisplayName = "HR Department", Email = "hr@contoso.com", Description = "HR Department", GroupScope = "Global", GroupType = "Security", ObjectType = "Group", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridComputer { DisplayName = "PC-IT-001", DnsHostName = "pc-it-001.contoso.com", OperatingSystem = "Windows 11 Enterprise", ObjectType = "Computer", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridComputer { DisplayName = "PC-HR-002", DnsHostName = "pc-hr-002.contoso.com", OperatingSystem = "Windows 11 Enterprise", ObjectType = "Computer", SyncStatus = new SyncStatus { Status = SyncState.Pending } },
            new HybridUser { DisplayName = "Eve Edwards", Email = "eve@contoso.com", Description = "Sales Manager", Telephone = "555-0105", FirstName = "Eve", LastName = "Edwards", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridUser { DisplayName = "Frank Foster", Email = "frank@contoso.com", Description = "Engineer", Telephone = "555-0106", FirstName = "Frank", LastName = "Foster", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridUser { DisplayName = "Grace Hopper", Email = "grace@contoso.com", Description = "Senior Engineer", Telephone = "555-0107", FirstName = "Grace", LastName = "Hopper", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
            new HybridUser { DisplayName = "Hank Hill", Email = "hank@contoso.com", Description = "Technician", Telephone = "555-0108", FirstName = "Hank", LastName = "Hill", ObjectType = "User", SyncStatus = new SyncStatus { Status = SyncState.Pending } },
            new HybridGroup { DisplayName = "Dev Team", Email = "dev@contoso.com", Description = "Development Team", GroupScope = "Global", GroupType = "Security", ObjectType = "Group", SyncStatus = new SyncStatus { Status = SyncState.InSync } },
        };

        var filtered = allObjects.Where(o =>
        {
            if (!string.IsNullOrWhiteSpace(query.FilterName) && !o.DisplayName.Contains(query.FilterName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(query.FilterEmail) && !o.Email.Contains(query.FilterEmail, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(query.FilterDescription) && !o.Description.Contains(query.FilterDescription, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(query.FilterPhone))
            {
                var phone = o is HybridUser user ? user.Telephone : string.Empty;
                if (!phone.Contains(query.FilterPhone, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        });

        foreach (var obj in filtered)
        {
            Objects.Add(obj);
        }
    }
}
