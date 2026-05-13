using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace HybridADManager.Models;

public partial class DirectoryNode : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _distinguishedName = string.Empty;

    [ObservableProperty]
    private NodeType _nodeType;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<DirectoryNode> _children = new();

    [ObservableProperty]
    private DirectoryObject? _directoryObject;

    public bool HasDummyChild => Children.Count == 1 && Children[0].DisplayName == "Loading...";
}

public enum NodeType
{
    Domain,
    BuiltinContainer,
    ComputersContainer,
    DomainControllersContainer,
    ForeignSecurityPrincipalsContainer,
    ManagedServiceAccountsContainer,
    UsersContainer,
    CustomOU,
    SavedQueries
}
