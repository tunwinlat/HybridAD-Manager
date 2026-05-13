using CommunityToolkit.Mvvm.ComponentModel;

namespace HybridADManager.Models;

public enum SyncState
{
    Unknown,
    InSync,
    Pending,
    CloudOnly,
    SyncError
}

public partial class SyncStatus : ObservableObject
{
    [ObservableProperty]
    private SyncState _status = SyncState.Unknown;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public string DisplayText => Status switch
    {
        SyncState.InSync => "In Sync",
        SyncState.Pending => "Pending",
        SyncState.CloudOnly => "Cloud Only",
        SyncState.SyncError => "Sync Error",
        _ => "Unknown"
    };

    public string IconGlyph => Status switch
    {
        SyncState.InSync => "\uE930",
        SyncState.Pending => "\uE823",
        SyncState.CloudOnly => "\uE753",
        SyncState.SyncError => "\uE783",
        _ => "\uE9CE"
    };
}
