using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using System.Windows;

namespace HybridADManager.ViewModels;

public partial class HybridStatusTabViewModel : ObservableObject
{
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private HybridUser _user;

    [ObservableProperty]
    private HybridSyncState _syncDetails = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasLastSyncTime;

    [ObservableProperty]
    private bool _hasSyncErrors;

    [ObservableProperty]
    private bool _canForceSync;

    [ObservableProperty]
    private string _directorySourceText = "Unknown";

    public HybridStatusTabViewModel(HybridUser user, IGraphService graphService)
    {
        _user = user;
        _graphService = graphService;
        LoadSyncDetailsAsync();
    }

    private async void LoadSyncDetailsAsync()
    {
        if (string.IsNullOrEmpty(_user.EntraObjectId) && string.IsNullOrEmpty(_user.Upn))
            return;

        IsLoading = true;
        try
        {
            var userId = _user.EntraObjectId ?? _user.Upn;
            var state = await _graphService.GetSyncStateAsync(userId);

            if (state != null)
            {
                SyncDetails = state;
                HasLastSyncTime = state.LastDirSyncTime.HasValue;
                HasSyncErrors = state.SyncErrors?.Count > 0;
                CanForceSync = state.IsSynced;

                DirectorySourceText = state.IsSynced
                    ? "Windows Server AD (synchronized to Microsoft Entra ID)"
                    : !string.IsNullOrEmpty(state.EntraObjectId)
                        ? "Microsoft Entra ID (cloud-only)"
                        : "Not found in Microsoft Entra ID";

                // Update the user's sync status based on Graph data
                User.SyncStatus.Status = DetermineSyncState(state);
            }
        }
        catch
        {
            // Keep default values on error
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ForceSyncAsync()
    {
        try
        {
            await _graphService.ForceSyncAsync();
            MessageBox.Show("Azure AD Connect sync has been triggered. Changes may take a few minutes to propagate.",
                           "Sync Triggered", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to trigger sync: {ex.Message}",
                           "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static SyncState DetermineSyncState(HybridSyncState state)
    {
        if (state.SyncErrors?.Count > 0)
            return SyncState.SyncError;
        if (state.IsSynced)
            return SyncState.InSync;
        if (!string.IsNullOrEmpty(state.EntraObjectId))
            return SyncState.CloudOnly;
        return SyncState.Unknown;
    }
}
