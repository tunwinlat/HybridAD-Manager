using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using System.Collections.ObjectModel;

namespace HybridADManager.ViewModels;

public partial class LicensesTabViewModel : ObservableObject
{
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private HybridUser _user;

    [ObservableProperty]
    private ObservableCollection<LicenseRowViewModel> _licenses = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasLicenses;

    public LicensesTabViewModel(HybridUser user, IGraphService graphService)
    {
        _user = user;
        _graphService = graphService;
        LoadLicensesAsync();
    }

    private async void LoadLicensesAsync()
    {
        if (string.IsNullOrEmpty(_user.EntraObjectId))
        {
            ErrorMessage = "User not found in Entra ID.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var skus = await _graphService.GetSubscribedSkusAsync();
            var userLicenses = await _graphService.GetUserLicensesAsync(_user.EntraObjectId);
            var userLicenseSet = new HashSet<string>(userLicenses);

            Licenses.Clear();

            foreach (var sku in skus.Values)
            {
                var servicePlans = new ObservableCollection<ServicePlanRowViewModel>();
                foreach (var plan in sku.ServicePlans)
                {
                    servicePlans.Add(new ServicePlanRowViewModel
                    {
                        ServicePlanId = plan.ServicePlanId,
                        DisplayName = plan.DisplayName ?? plan.ServicePlanName,
                        ProvisioningStatus = plan.ProvisioningStatus,
                        IsEnabled = plan.AppliesTo
                    });
                }

                Licenses.Add(new LicenseRowViewModel
                {
                    SkuId = sku.SkuId,
                    DisplayName = sku.DisplayName ?? sku.SkuPartNumber,
                    IsAssigned = userLicenseSet.Contains(sku.SkuId),
                    TotalUnits = sku.TotalUnits,
                    ConsumedUnits = sku.ConsumedUnits,
                    ServicePlans = servicePlans
                });
            }

            HasLicenses = Licenses.Count > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load licenses: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleLicenseAsync(LicenseRowViewModel license)
    {
        if (string.IsNullOrEmpty(_user.EntraObjectId))
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            if (license.IsAssigned)
            {
                await _graphService.AssignLicenseAsync(_user.EntraObjectId, new[] { license.SkuId });
            }
            else
            {
                await _graphService.RemoveLicenseAsync(_user.EntraObjectId, new[] { license.SkuId });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update license: {ex.Message}";
            // Revert the local state on failure
            license.IsAssigned = !license.IsAssigned;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLicensesAsync()
    {
        Licenses.Clear();
        LoadLicensesAsync();
    }
}

public partial class LicenseRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _skuId = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isAssigned;

    [ObservableProperty]
    private int _totalUnits;

    [ObservableProperty]
    private int _consumedUnits;

    [ObservableProperty]
    private ObservableCollection<ServicePlanRowViewModel> _servicePlans = new();

    [ObservableProperty]
    private bool _isExpanded;
}

public partial class ServicePlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _servicePlanId = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _provisioningStatus = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;
}
