using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using HybridADManager.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class DirectoryTreeViewModel : ObservableObject
{
    private readonly IActiveDirectoryService? _adService;
    private readonly ISettingsService? _settingsService;

    [ObservableProperty]
    private ObservableCollection<DirectoryNode> _nodes = new();

    [ObservableProperty]
    private DirectoryNode? _selectedNode;

    public event EventHandler<DirectoryNode?>? NodeSelected;

    public DirectoryTreeViewModel()
    {
        _adService = App.Current?.Services?.GetService(typeof(IActiveDirectoryService)) as IActiveDirectoryService;
        _settingsService = App.Current?.Services?.GetService(typeof(ISettingsService)) as ISettingsService;
        LoadDomainAsync();
    }

    partial void OnSelectedNodeChanged(DirectoryNode? value)
    {
        NodeSelected?.Invoke(this, value);
    }

    [RelayCommand]
    private async Task ExpandNodeAsync(DirectoryNode node)
    {
        if (!node.IsExpanded) return;

        if (node.HasDummyChild)
        {
            node.IsLoading = true;
            node.Children.Clear();

            try
            {
                if (_adService != null && !string.IsNullOrEmpty(node.DistinguishedName))
                {
                    var children = await _adService.GetChildOUsAsync(node.DistinguishedName);
                    foreach (var child in children)
                    {
                        child.Children.Add(new DirectoryNode
                        {
                            DisplayName = "Loading...",
                            NodeType = NodeType.CustomOU
                        });
                        node.Children.Add(child);
                    }
                }

                // If no children from AD, try dummy data fallback
                if (node.Children.Count == 0)
                {
                    LoadDummyChildren(node);
                }
            }
            catch
            {
                LoadDummyChildren(node);
            }
            finally
            {
                node.IsLoading = false;
            }
        }
    }

    public void RefreshDomain()
    {
        Nodes.Clear();
        LoadDomainAsync();
    }

    private async void LoadDomainAsync()
    {
        try
        {
            if (_adService != null)
            {
                var domainName = await _adService.GetCurrentDomainNameAsync();

                if (!string.IsNullOrEmpty(domainName))
                {
                    var rootDn = $"DC={domainName.Replace(".", ",DC=")}";
                    var containers = await _adService.GetDomainContainersAsync(domainName);

                    var domainNode = new DirectoryNode
                    {
                        DisplayName = domainName,
                        DistinguishedName = rootDn,
                        NodeType = NodeType.Domain,
                        IsExpanded = true
                    };

                    // Add containers
                    foreach (var container in containers)
                    {
                        container.Children.Add(new DirectoryNode
                        {
                            DisplayName = "Loading...",
                            NodeType = NodeType.CustomOU
                        });
                        domainNode.Children.Add(container);
                    }

                    // Add Saved Queries
                    var savedQueriesNode = new DirectoryNode
                    {
                        DisplayName = "Saved Queries",
                        NodeType = NodeType.SavedQueries,
                        Children = new ObservableCollection<DirectoryNode>()
                    };
                    domainNode.Children.Add(savedQueriesNode);

                    Nodes.Add(domainNode);
                    await PopulateSavedQueriesAsync(savedQueriesNode);
                    return;
                }
            }

            // No AD available - use dummy data
            LoadDummyData();
        }
        catch
        {
            LoadDummyData();
        }
    }

    private void LoadDummyData()
    {
        var savedQueriesNode = new DirectoryNode
        {
            DisplayName = "Saved Queries",
            NodeType = NodeType.SavedQueries,
            Children = new ObservableCollection<DirectoryNode>()
        };

        var domainNode = new DirectoryNode
        {
            DisplayName = "contoso.com (demo)",
            NodeType = NodeType.Domain,
            IsExpanded = true,
            Children = new ObservableCollection<DirectoryNode>
            {
                CreateContainerNode("Builtin", NodeType.BuiltinContainer),
                CreateContainerNode("Computers", NodeType.ComputersContainer),
                CreateContainerNode("Domain Controllers", NodeType.DomainControllersContainer),
                CreateContainerNode("ForeignSecurityPrincipals", NodeType.ForeignSecurityPrincipalsContainer),
                CreateContainerNode("Managed Service Accounts", NodeType.ManagedServiceAccountsContainer),
                new DirectoryNode
                {
                    DisplayName = "Users",
                    NodeType = NodeType.UsersContainer,
                    IsExpanded = true,
                    Children = new ObservableCollection<DirectoryNode>
                    {
                        CreateUserNode("Alice Alison", "alice@contoso.com", SyncState.InSync),
                        CreateUserNode("Bob Builder", "bob@contoso.com", SyncState.Pending),
                        CreateUserNode("Charlie Chaplin", "charlie@contoso.com", SyncState.CloudOnly),
                        CreateUserNode("Diana Prince", "diana@contoso.com", SyncState.SyncError),
                        CreateGroupNode("IT Support", "it@contoso.com", SyncState.InSync),
                        CreateGroupNode("HR Department", "hr@contoso.com", SyncState.InSync),
                        CreateComputerNode("PC-IT-001", SyncState.InSync),
                        CreateComputerNode("PC-HR-002", SyncState.Pending),
                    }
                },
                new DirectoryNode
                {
                    DisplayName = "Sales",
                    NodeType = NodeType.CustomOU,
                    Children = new ObservableCollection<DirectoryNode>
                    {
                        new DirectoryNode { DisplayName = "Loading...", NodeType = NodeType.CustomOU }
                    }
                },
                new DirectoryNode
                {
                    DisplayName = "Engineering",
                    NodeType = NodeType.CustomOU,
                    Children = new ObservableCollection<DirectoryNode>
                    {
                        new DirectoryNode { DisplayName = "Loading...", NodeType = NodeType.CustomOU }
                    }
                },
                savedQueriesNode
            }
        };

        Nodes.Add(domainNode);
        _ = PopulateSavedQueriesAsync(savedQueriesNode);
    }

    private void LoadDummyChildren(DirectoryNode node)
    {
        if (node.DisplayName == "Sales")
        {
            node.Children.Add(CreateUserNode("Eve Edwards", "eve@contoso.com", SyncState.InSync));
            node.Children.Add(CreateUserNode("Frank Foster", "frank@contoso.com", SyncState.InSync));
        }
        else if (node.DisplayName == "Engineering")
        {
            node.Children.Add(CreateUserNode("Grace Hopper", "grace@contoso.com", SyncState.InSync));
            node.Children.Add(CreateUserNode("Hank Hill", "hank@contoso.com", SyncState.Pending));
            node.Children.Add(CreateGroupNode("Dev Team", "dev@contoso.com", SyncState.InSync));
        }
    }

    private DirectoryNode CreateContainerNode(string name, NodeType type)
    {
        return new DirectoryNode
        {
            DisplayName = name,
            NodeType = type,
            Children = new ObservableCollection<DirectoryNode>
            {
                new DirectoryNode { DisplayName = "Loading...", NodeType = NodeType.CustomOU }
            }
        };
    }

    private DirectoryNode CreateUserNode(string name, string email, SyncState sync)
    {
        return new DirectoryNode
        {
            DisplayName = name,
            NodeType = NodeType.CustomOU,
            DirectoryObject = new HybridUser
            {
                DisplayName = name,
                Email = email,
                FirstName = name.Split(' ')[0],
                LastName = name.Split(' ')[1],
                ObjectType = "User",
                SyncStatus = new SyncStatus { Status = sync }
            }
        };
    }

    private DirectoryNode CreateGroupNode(string name, string email, SyncState sync)
    {
        return new DirectoryNode
        {
            DisplayName = name,
            NodeType = NodeType.CustomOU,
            DirectoryObject = new HybridGroup
            {
                DisplayName = name,
                Email = email,
                ObjectType = "Group",
                GroupScope = "Global",
                GroupType = "Security",
                SyncStatus = new SyncStatus { Status = sync }
            }
        };
    }

    private DirectoryNode CreateComputerNode(string name, SyncState sync)
    {
        return new DirectoryNode
        {
            DisplayName = name,
            NodeType = NodeType.CustomOU,
            DirectoryObject = new HybridComputer
            {
                DisplayName = name,
                DnsHostName = $"{name.ToLowerInvariant()}.contoso.com",
                ObjectType = "Computer",
                OperatingSystem = "Windows 11 Enterprise",
                SyncStatus = new SyncStatus { Status = sync }
            }
        };
    }

    private async Task PopulateSavedQueriesAsync(DirectoryNode savedQueriesNode)
    {
        if (_settingsService == null) return;

        savedQueriesNode.Children.Clear();

        try
        {
            var queries = await _settingsService.GetSavedQueriesAsync();
            foreach (var query in queries)
            {
                savedQueriesNode.Children.Add(new DirectoryNode
                {
                    DisplayName = query.Name,
                    DistinguishedName = $"QUERY:{query.Id}",
                    NodeType = NodeType.SavedQueries,
                    DirectoryObject = null
                });
            }
        }
        catch { }
    }

    private DirectoryNode? FindSavedQueriesNode()
    {
        foreach (var node in Nodes)
        {
            var found = FindSavedQueriesNodeRecursive(node);
            if (found != null) return found;
        }
        return null;
    }

    private static DirectoryNode? FindSavedQueriesNodeRecursive(DirectoryNode node)
    {
        if (node.NodeType == NodeType.SavedQueries && string.IsNullOrEmpty(node.DistinguishedName))
            return node;

        foreach (var child in node.Children)
        {
            var found = FindSavedQueriesNodeRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    [RelayCommand]
    private async Task RefreshSavedQueriesAsync()
    {
        try
        {
            var savedQueriesNode = FindSavedQueriesNode();
            if (savedQueriesNode != null)
            {
                await PopulateSavedQueriesAsync(savedQueriesNode);
            }
        }
        catch { }
    }

    [RelayCommand]
    private void NewSavedQuery()
    {
        var dialog = new SavedQueryDialog
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.SavedQuery != null)
        {
            _ = SaveQueryAndRefreshAsync(dialog.SavedQuery);
        }
    }

    private async Task SaveQueryAndRefreshAsync(SavedQuery query)
    {
        try
        {
            if (_settingsService != null)
            {
                await _settingsService.SaveSavedQueryAsync(query);
            }
            await RefreshSavedQueriesAsync();
        }
        catch { }
    }
}
