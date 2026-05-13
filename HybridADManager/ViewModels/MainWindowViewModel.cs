using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using HybridADManager.Views.PropertySheets;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private string _windowTitle = "HybridAD-Manager";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _domainName = "";

    [ObservableProperty]
    private string _loggedInUser = "";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _authButtonText = "🔐 Sign In";

    [ObservableProperty]
    private int _selectedObjectCount;

    [ObservableProperty]
    private DirectoryTreeViewModel _treeViewModel = new();

    [ObservableProperty]
    private ObjectListViewModel _listViewModel = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = "";

    public MainWindowViewModel()
    {
        _authService = App.Current.Services.GetService(typeof(IAuthenticationService)) as IAuthenticationService
            ?? new AuthenticationService();
        _graphService = App.Current.Services.GetService(typeof(IGraphService)) as IGraphService
            ?? new GraphService(_authService);

        _authService.AuthenticationStateChanged += OnAuthStateChanged;
        TreeViewModel.NodeSelected += OnNodeSelected;

        CheckAuthStateAsync();
    }

    private async void CheckAuthStateAsync()
    {
        var isAuth = await _authService.IsAuthenticatedAsync();
        if (isAuth)
        {
            UpdateAuthState(true, "admin@contoso.com", null);
        }
    }

    private void OnAuthStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
    {
        UpdateAuthState(e.IsAuthenticated, e.AccountUsername, e.TenantId);
    }

    private void UpdateAuthState(bool isAuthenticated, string? username, string? tenantId)
    {
        IsAuthenticated = isAuthenticated;
        AuthButtonText = isAuthenticated ? $"✅ {username}" : "🔐 Sign In";
        LoggedInUser = isAuthenticated ? $"Entra: {username}" : "Entra: Not signed in";
    }

    private void OnNodeSelected(object? sender, DirectoryNode? node)
    {
        if (node == null) return;

        StatusText = $"Selected: {node.DisplayName}";
        ListViewModel.LoadObjectsForNode(node);
    }

    [RelayCommand]
    private async Task ToggleAuthenticationAsync()
    {
        if (IsAuthenticated)
        {
            await _authService.SignOutAsync();
        }
        else
        {
            IsBusy = true;
            BusyMessage = "Signing in to Microsoft Entra ID...";
            try
            {
                var result = await _authService.AuthenticateAsync();
                if (result != null)
                {
                    await _graphService.EnsureAuthenticatedAsync();
                }
            }
            finally
            {
                IsBusy = false;
                BusyMessage = "";
            }
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        TreeViewModel.RefreshDomain();
    }

    [RelayCommand]
    private void Find()
    {
        // TODO: Open Find dialog
    }

    [RelayCommand]
    private void NewUser()
    {
        // TODO: Open New User dialog
    }

    [RelayCommand]
    private void NewGroup()
    {
        // TODO: Open New Group dialog
    }

    [RelayCommand]
    private void Delete()
    {
        // TODO: Confirm and delete selected objects
    }

    [RelayCommand]
    private void Properties()
    {
        var selectedObject = ListViewModel.SelectedObject;
        if (selectedObject == null) return;

        OpenPropertySheet(selectedObject);
    }

    private void OpenPropertySheet(DirectoryObject directoryObject)
    {
        var graphService = App.Current.Services.GetService(typeof(IGraphService)) as IGraphService
            ?? new GraphService(_authService);

        Window? propertySheet = directoryObject switch
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
}
