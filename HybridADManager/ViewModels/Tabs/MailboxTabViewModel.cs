using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;

namespace HybridADManager.ViewModels;

public partial class MailboxTabViewModel : ObservableObject
{
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private HybridUser _user;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasMailboxData;

    [ObservableProperty]
    private bool _autoReplyEnabled;

    [ObservableProperty]
    private bool _autoReplyScheduled;

    [ObservableProperty]
    private bool _autoReplyDisabled;

    [ObservableProperty]
    private string _autoReplyInternalMessage = string.Empty;

    [ObservableProperty]
    private string _autoReplyExternalMessage = string.Empty;

    [ObservableProperty]
    private string _forwardingAddress = string.Empty;

    [ObservableProperty]
    private string _externalAudience = "all";

    [ObservableProperty]
    private string _primarySmtp = string.Empty;

    [ObservableProperty]
    private bool _hideFromAddressLists;

    public MailboxTabViewModel(HybridUser user, IGraphService graphService)
    {
        _user = user;
        _graphService = graphService;
        LoadMailboxSettingsAsync();
    }

    private async void LoadMailboxSettingsAsync()
    {
        if (string.IsNullOrEmpty(_user.EntraObjectId))
        {
            ErrorMessage = "User not found in Entra ID.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _graphService.GetMailboxSettingsAsync(_user.EntraObjectId);

            if (result != null)
            {
                AutoReplyEnabled = result.AutomaticRepliesSetting?.Status == "alwaysenabled";
                AutoReplyScheduled = result.AutomaticRepliesSetting?.Status == "scheduled";
                AutoReplyDisabled = !AutoReplyEnabled && !AutoReplyScheduled;
                AutoReplyInternalMessage = result.AutomaticRepliesSetting?.InternalReplyMessage ?? string.Empty;
                AutoReplyExternalMessage = result.AutomaticRepliesSetting?.ExternalReplyMessage ?? string.Empty;
                ForwardingAddress = result.ForwardingSmtpAddress ?? string.Empty;
                ExternalAudience = result.ExternalAudience ?? "all";
                HasMailboxData = true;
            }

            PrimarySmtp = _user.Email;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveMailboxSettingsAsync()
    {
        if (string.IsNullOrEmpty(_user.EntraObjectId))
        {
            ErrorMessage = "User not found in Entra ID.";
            return;
        }

        IsLoading = true;
        try
        {
            string status;
            if (AutoReplyEnabled)
                status = "alwaysenabled";
            else if (AutoReplyScheduled)
                status = "scheduled";
            else
                status = "disabled";

            var settings = new MailboxSettings
            {
                AutomaticRepliesSetting = new AutomaticRepliesSetting
                {
                    Status = status,
                    InternalReplyMessage = AutoReplyInternalMessage,
                    ExternalReplyMessage = AutoReplyExternalMessage
                },
                ForwardingSmtpAddress = string.IsNullOrEmpty(ForwardingAddress) ? null : ForwardingAddress,
                ExternalAudience = ExternalAudience
            };

            await _graphService.UpdateMailboxSettingsAsync(_user.EntraObjectId, settings);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
