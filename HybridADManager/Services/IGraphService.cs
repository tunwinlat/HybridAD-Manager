using HybridADManager.Models;

namespace HybridADManager.Services;

public interface IGraphService
{
    Task<bool> EnsureAuthenticatedAsync();
    Task<HybridUser?> GetUserByUpnAsync(string upn);
    Task<HybridUser?> GetUserByImmutableIdAsync(string immutableId);
    Task<HybridGroup?> GetGroupByDisplayNameAsync(string displayName);
    Task<IEnumerable<HybridUser>> GetCloudOnlyUsersAsync();
    Task<IEnumerable<HybridGroup>> GetCloudOnlyGroupsAsync();
    Task<Dictionary<string, M365License>> GetSubscribedSkusAsync();
    Task<IEnumerable<string>> GetUserLicensesAsync(string userId);
    Task AssignLicenseAsync(string userId, IEnumerable<string> skuIds);
    Task RemoveLicenseAsync(string userId, IEnumerable<string> skuIds);
    Task ForceSyncAsync();
    Task<HybridSyncState> GetSyncStateAsync(string userId);
    Task<MailboxSettings?> GetMailboxSettingsAsync(string userId);
    Task UpdateMailboxSettingsAsync(string userId, MailboxSettings settings);
    Task UpdateUserProxyAddressesAsync(string userId, List<string> proxyAddresses, string? primarySmtp);
}

public class MailboxSettings
{
    public AutomaticRepliesSetting? AutomaticRepliesSetting { get; set; }
    public string? ForwardingSmtpAddress { get; set; }
    public string? ExternalAudience { get; set; }
    public string? UserPurpose { get; set; }
}

public class AutomaticRepliesSetting
{
    public string Status { get; set; } = "disabled";
    public string? ScheduledStartDateTime { get; set; }
    public string? ScheduledEndDateTime { get; set; }
    public string? InternalReplyMessage { get; set; }
    public string? ExternalReplyMessage { get; set; }
}

public class HybridSyncState
{
    public bool IsSynced { get; set; }
    public string? EntraObjectId { get; set; }
    public string? ImmutableId { get; set; }
    public DateTimeOffset? LastDirSyncTime { get; set; }
    public string? OnPremisesDistinguishedName { get; set; }
    public string? OnPremisesDomainName { get; set; }
    public string? OnPremisesSamAccountName { get; set; }
    public string? OnPremisesSecurityIdentifier { get; set; }
    public string? OnPremisesSyncEnabled { get; set; }
    public List<SyncError>? SyncErrors { get; set; }
}

public class SyncError
{
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset? OccurredDateTime { get; set; }
}

public class M365License
{
    public string SkuId { get; set; } = string.Empty;
    public string SkuPartNumber { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public int TotalUnits { get; set; }
    public int ConsumedUnits { get; set; }
    public List<ServicePlan> ServicePlans { get; set; } = new();
}

public class ServicePlan
{
    public string ServicePlanId { get; set; } = string.Empty;
    public string ServicePlanName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string ProvisioningStatus { get; set; } = string.Empty;
    public bool AppliesTo { get; set; }
}
