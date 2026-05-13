using HybridADManager.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace HybridADManager.Services;

public class GraphService : IGraphService
{
    private readonly IAuthenticationService _authService;
    private GraphServiceClient? _graphClient;
    private static readonly Dictionary<string, string> SkuFriendlyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENTERPRISEPACK"] = "Office 365 E3",
        ["ENTERPRISEPREMIUM"] = "Office 365 E5",
        ["SPE_E3"] = "Microsoft 365 E3",
        ["SPE_E5"] = "Microsoft 365 E5",
        ["STANDARDPACK"] = "Office 365 E1",
        ["DESKLESSPACK"] = "Office 365 F3",
        ["FLOW_FREE"] = "Microsoft Power Automate Free",
        ["POWER_BI_PRO"] = "Power BI Pro",
        ["WIN10_VDA_E5"] = "Windows 10/11 Enterprise E5",
        ["AAD_PREMIUM"] = "Azure AD Premium P1",
        ["AAD_PREMIUM_P2"] = "Azure AD Premium P2",
        ["EMS"] = "Enterprise Mobility + Security E3",
        ["EMSPREMIUM"] = "Enterprise Mobility + Security E5",
        ["TEAMS1"] = "Microsoft Teams",
        ["EXCHANGEENTERPRISE"] = "Exchange Online (Plan 2)",
        ["EXCHANGESTANDARD"] = "Exchange Online (Plan 1)",
        ["MCOMEETADV"] = "Microsoft Teams Audio Conferencing",
        ["MCOEV"] = "Microsoft Teams Phone Standard",
    };

    public GraphService(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<bool> EnsureAuthenticatedAsync()
    {
        var authResult = await _authService.AcquireTokenSilentAsync();
        if (authResult == null)
        {
            authResult = await _authService.AuthenticateAsync();
        }

        if (authResult != null)
        {
            _graphClient = new GraphServiceClient(new TokenCredentialAuthProvider(authResult.AccessToken));
            return true;
        }

        return false;
    }

    public async Task<HybridUser?> GetUserByUpnAsync(string upn)
    {
        if (_graphClient == null) return null;

        try
        {
            var user = await _graphClient.Users[upn]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                        "jobTitle", "department", "companyName", "officeLocation", "businessPhones",
                        "onPremisesImmutableId", "onPremisesDistinguishedName", "onPremisesDomainName",
                        "onPremisesSamAccountName", "onPremisesSecurityIdentifier", "onPremisesSyncEnabled",
                        "lastPasswordChangeDateTime", "accountEnabled", "createdDateTime",
                        "onPremisesProvisioningErrors"
                    };
                });

            return user == null ? null : MapGraphUserToHybridUser(user);
        }
        catch
        {
            return null;
        }
    }

    public async Task<HybridUser?> GetUserByImmutableIdAsync(string immutableId)
    {
        if (_graphClient == null) return null;

        try
        {
            var users = await _graphClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"onPremisesImmutableId eq '{immutableId}'";
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                        "jobTitle", "department", "companyName", "officeLocation", "businessPhones",
                        "onPremisesImmutableId", "onPremisesDistinguishedName", "onPremisesDomainName",
                        "onPremisesSamAccountName", "onPremisesSecurityIdentifier", "onPremisesSyncEnabled",
                        "lastPasswordChangeDateTime", "accountEnabled", "createdDateTime"
                    };
                });

            var user = users?.Value?.FirstOrDefault();
            return user == null ? null : MapGraphUserToHybridUser(user);
        }
        catch
        {
            return null;
        }
    }

    public async Task<HybridGroup?> GetGroupByDisplayNameAsync(string displayName)
    {
        if (_graphClient == null) return null;

        try
        {
            var groups = await _graphClient.Groups
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{displayName}'";
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "description", "mail", "groupTypes",
                        "onPremisesSyncEnabled", "onPremisesDomainName", "onPremisesSamAccountName",
                        "onPremisesSecurityIdentifier", "membershipRule"
                    };
                });

            var group = groups?.Value?.FirstOrDefault();
            return group == null ? null : MapGraphGroupToHybridGroup(group);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<HybridUser>> GetCloudOnlyUsersAsync()
    {
        if (_graphClient == null) return Enumerable.Empty<HybridUser>();

        try
        {
            var users = await _graphClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = "onPremisesSyncEnabled ne true";
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "givenName", "surname", "mail", "userPrincipalName",
                        "jobTitle", "department", "companyName", "accountEnabled", "createdDateTime"
                    };
                    requestConfiguration.QueryParameters.Top = 100;
                });

            return users?.Value?.Select(MapGraphUserToHybridUser) ?? Enumerable.Empty<HybridUser>();
        }
        catch
        {
            return Enumerable.Empty<HybridUser>();
        }
    }

    public async Task<IEnumerable<HybridGroup>> GetCloudOnlyGroupsAsync()
    {
        if (_graphClient == null) return Enumerable.Empty<HybridGroup>();

        try
        {
            var groups = await _graphClient.Groups
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = "onPremisesSyncEnabled ne true";
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "displayName", "description", "mail", "groupTypes"
                    };
                    requestConfiguration.QueryParameters.Top = 100;
                });

            return groups?.Value?.Select(MapGraphGroupToHybridGroup) ?? Enumerable.Empty<HybridGroup>();
        }
        catch
        {
            return Enumerable.Empty<HybridGroup>();
        }
    }

    public async Task<Dictionary<string, M365License>> GetSubscribedSkusAsync()
    {
        var result = new Dictionary<string, M365License>();
        if (_graphClient == null) return result;

        try
        {
            var skus = await _graphClient.SubscribedSkus.GetAsync();
            if (skus?.Value == null) return result;

            foreach (var sku in skus.Value)
            {
                if (sku.SkuId == null || sku.SkuPartNumber == null) continue;

                var license = new M365License
                {
                    SkuId = sku.SkuId.ToString()!,
                    SkuPartNumber = sku.SkuPartNumber,
                    DisplayName = GetFriendlySkuName(sku.SkuPartNumber),
                    TotalUnits = sku.PrepaidUnits?.Enabled ?? 0 + (sku.PrepaidUnits?.Suspended ?? 0),
                    ConsumedUnits = (int)(sku.ConsumedUnits ?? 0),
                    ServicePlans = sku.ServicePlans?.Select(sp => new ServicePlan
                    {
                        ServicePlanId = sp.ServicePlanId?.ToString() ?? "",
                        ServicePlanName = sp.ServicePlanName ?? "",
                        ProvisioningStatus = sp.ProvisioningStatus ?? "",
                        AppliesTo = sp.AppliesTo == "User"
                    }).ToList() ?? new List<ServicePlan>()
                };

                result[license.SkuId] = license;
            }
        }
        catch
        {
            // Return empty on error
        }

        return result;
    }

    public async Task<IEnumerable<string>> GetUserLicensesAsync(string userId)
    {
        if (_graphClient == null) return Enumerable.Empty<string>();

        try
        {
            var user = await _graphClient.Users[userId]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "assignedLicenses" };
                });

            return user?.AssignedLicenses?.Select(l => l.SkuId?.ToString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s)) ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task AssignLicenseAsync(string userId, IEnumerable<string> skuIds)
    {
        if (_graphClient == null) return;

        var addLicenses = skuIds.Select(skuId => new AssignedLicense
        {
            SkuId = Guid.Parse(skuId),
            DisabledPlans = new List<Guid>()
        }).ToList();

        var requestBody = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
        {
            AddLicenses = addLicenses,
            RemoveLicenses = new List<Guid>()
        };

        await _graphClient.Users[userId].AssignLicense.PostAsync(requestBody);
    }

    public async Task RemoveLicenseAsync(string userId, IEnumerable<string> skuIds)
    {
        if (_graphClient == null) return;

        var removeLicenses = skuIds.Select(Guid.Parse).ToList();

        var requestBody = new Microsoft.Graph.Users.Item.AssignLicense.AssignLicensePostRequestBody
        {
            AddLicenses = new List<AssignedLicense>(),
            RemoveLicenses = removeLicenses
        };

        await _graphClient.Users[userId].AssignLicense.PostAsync(requestBody);
    }

    public async Task ForceSyncAsync()
    {
        // Force sync requires either:
        // 1. Running on the AAD Connect server and calling Start-ADSyncSyncCycle
        // 2. Using the MS Graph sync API (requires specific permissions)
        // For now, this is a placeholder that will show a message
        await Task.Delay(100);
    }

    public async Task<HybridSyncState> GetSyncStateAsync(string userId)
    {
        var state = new HybridSyncState();
        if (_graphClient == null) return state;

        try
        {
            var user = await _graphClient.Users[userId]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[]
                    {
                        "id", "onPremisesImmutableId", "onPremisesDistinguishedName",
                        "onPremisesDomainName", "onPremisesSamAccountName",
                        "onPremisesSecurityIdentifier", "onPremisesSyncEnabled",
                        "onPremisesProvisioningErrors"
                    };
                });

            if (user == null) return state;

            state.EntraObjectId = user.Id;
            state.ImmutableId = user.OnPremisesImmutableId;
            state.OnPremisesDistinguishedName = user.OnPremisesDistinguishedName;
            state.OnPremisesDomainName = user.OnPremisesDomainName;
            state.OnPremisesSamAccountName = user.OnPremisesSamAccountName;
            state.OnPremisesSecurityIdentifier = user.OnPremisesSecurityIdentifier;
            state.OnPremisesSyncEnabled = user.OnPremisesSyncEnabled?.ToString();
            state.IsSynced = !string.IsNullOrEmpty(user.OnPremisesImmutableId) ||
                             user.OnPremisesSyncEnabled == true;

            if (user.OnPremisesProvisioningErrors?.Count > 0)
            {
                state.SyncErrors = user.OnPremisesProvisioningErrors
                    .Select(e => new SyncError
                    {
                        ErrorCode = e.Category,
                        Message = e.PropertyCausingError
                    }).ToList();
            }
        }
        catch
        {
            // Return default state on error
        }

        return state;
    }

    // Helper methods
    private static HybridUser MapGraphUserToHybridUser(User user)
    {
        return new HybridUser
        {
            DisplayName = user.DisplayName ?? "",
            FirstName = user.GivenName ?? "",
            LastName = user.Surname ?? "",
            Email = user.Mail ?? "",
            Upn = user.UserPrincipalName ?? "",
            JobTitle = user.JobTitle ?? "",
            Department = user.Department ?? "",
            Company = user.CompanyName ?? "",
            Office = user.OfficeLocation ?? "",
            Telephone = user.BusinessPhones?.FirstOrDefault() ?? "",
            IsEnabled = user.AccountEnabled ?? true,
            EntraObjectId = user.Id,
            ImmutableId = user.OnPremisesImmutableId,
            SyncStatus = new SyncStatus
            {
                Status = DetermineSyncStatus(user)
            }
        };
    }

    private static HybridGroup MapGraphGroupToHybridGroup(Group group)
    {
        return new HybridGroup
        {
            DisplayName = group.DisplayName ?? "",
            Description = group.Description ?? "",
            Email = group.Mail ?? "",
            EntraObjectId = group.Id,
            MailEnabled = group.MailEnabled ?? false,
            GroupType = group.GroupTypes?.Contains("Unified") == true ? "Microsoft 365" :
                        group.GroupTypes?.Contains("DynamicMembership") == true ? "Dynamic" : "Security",
            SyncStatus = new SyncStatus
            {
                Status = group.OnPremisesSyncEnabled == true ? SyncState.InSync :
                         group.OnPremisesSyncEnabled == false ? SyncState.CloudOnly : SyncState.Unknown
            }
        };
    }

    private static SyncState DetermineSyncStatus(User user)
    {
        if (user.OnPremisesSyncEnabled == true)
            return SyncState.InSync;
        if (!string.IsNullOrEmpty(user.OnPremisesImmutableId))
            return SyncState.InSync;
        if (user.OnPremisesProvisioningErrors?.Count > 0)
            return SyncState.SyncError;
        if (user.OnPremisesSyncEnabled == false && string.IsNullOrEmpty(user.OnPremisesImmutableId))
            return SyncState.CloudOnly;
        return SyncState.Unknown;
    }

    private static string GetFriendlySkuName(string skuPartNumber)
    {
        return SkuFriendlyNames.TryGetValue(skuPartNumber, out var name)
            ? name
            : skuPartNumber;
    }
}

// Simple auth provider for Graph SDK
public class TokenCredentialAuthProvider : Microsoft.Graph.IAuthenticationProvider
{
    private readonly string _accessToken;

    public TokenCredentialAuthProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        return Task.CompletedTask;
    }
}
