using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace HybridADManager.ViewModels;

public partial class EmailAddressesTabViewModel : ObservableObject
{
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private HybridUser _user;

    [ObservableProperty]
    private ObservableCollection<ProxyAddressViewModel> _addresses = new();

    [ObservableProperty]
    private string _newAddress = string.Empty;

    [ObservableProperty]
    private string _selectedAddressType = "smtp";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ProxyAddressViewModel? _selectedAddress;

    [ObservableProperty]
    private bool _hasChanges;

    public EmailAddressesTabViewModel(HybridUser user, IGraphService graphService)
    {
        _user = user;
        _graphService = graphService;
        LoadAddresses();
    }

    private void LoadAddresses()
    {
        Addresses.Clear();

        foreach (var proxy in User.ProxyAddresses)
        {
            var vm = ParseProxyAddress(proxy);
            if (vm != null)
            {
                Addresses.Add(vm);
            }
        }

        if (!string.IsNullOrEmpty(User.Email))
        {
            bool found = false;
            foreach (var addr in Addresses)
            {
                if (string.Equals(addr.Address, User.Email, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Addresses.Insert(0, new ProxyAddressViewModel
                {
                    Address = User.Email,
                    Type = "SMTP",
                    IsPrimary = true
                });
            }
        }

        HasChanges = false;
    }

    private static ProxyAddressViewModel? ParseProxyAddress(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
            return null;

        var colonIndex = proxy.IndexOf(':');
        if (colonIndex < 0)
            return null;

        var type = proxy.Substring(0, colonIndex);
        var address = proxy.Substring(colonIndex + 1);

        var vm = new ProxyAddressViewModel
        {
            Address = address,
            Type = type
        };

        if (string.Equals(type, "SMTP", StringComparison.OrdinalIgnoreCase))
        {
            vm.Type = "SMTP";
            vm.IsPrimary = true;
        }
        else if (string.Equals(type, "smtp", StringComparison.OrdinalIgnoreCase))
        {
            vm.Type = "smtp";
            vm.IsPrimary = false;
        }
        else if (string.Equals(type, "SIP", StringComparison.OrdinalIgnoreCase))
        {
            vm.Type = "SIP";
            vm.IsPrimary = false;
        }
        else
        {
            vm.Type = type;
            vm.IsPrimary = false;
        }

        return vm;
    }

    [RelayCommand]
    private void AddAddress()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewAddress))
        {
            ErrorMessage = "Please enter an email address.";
            return;
        }

        if (!Regex.IsMatch(NewAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }

        foreach (var existing in Addresses)
        {
            if (string.Equals(existing.Address, NewAddress, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This email address already exists.";
                return;
            }
        }

        var vm = new ProxyAddressViewModel
        {
            Address = NewAddress.Trim(),
            Type = SelectedAddressType,
            IsPrimary = false
        };

        Addresses.Add(vm);
        NewAddress = string.Empty;
        HasChanges = true;
    }

    [RelayCommand]
    private void RemoveAddress()
    {
        ErrorMessage = string.Empty;

        if (SelectedAddress == null)
            return;

        if (SelectedAddress.IsPrimary)
        {
            ErrorMessage = "Cannot remove primary address. Set another as primary first.";
            return;
        }

        Addresses.Remove(SelectedAddress);
        HasChanges = true;
    }

    [RelayCommand]
    private void SetPrimary()
    {
        ErrorMessage = string.Empty;

        if (SelectedAddress == null)
            return;

        foreach (var addr in Addresses)
        {
            if (addr.IsPrimary)
            {
                addr.IsPrimary = false;
                addr.Type = "smtp";
            }
        }

        SelectedAddress.IsPrimary = true;
        SelectedAddress.Type = "SMTP";
        HasChanges = true;
    }

    [RelayCommand]
    private async Task SaveAddressesAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var proxyAddresses = new List<string>();
            string? primarySmtp = null;

            foreach (var addr in Addresses)
            {
                proxyAddresses.Add($"{addr.Type}:{addr.Address}");

                if (addr.IsPrimary && string.Equals(addr.Type, "SMTP", StringComparison.OrdinalIgnoreCase))
                {
                    primarySmtp = addr.Address;
                }
            }

            var userId = User.EntraObjectId ?? User.Upn;
            await _graphService.UpdateUserProxyAddressesAsync(userId, proxyAddresses, primarySmtp);
            HasChanges = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save addresses: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class ProxyAddressViewModel : ObservableObject
{
    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _type = "smtp";

    [ObservableProperty]
    private bool _isPrimary;
}
