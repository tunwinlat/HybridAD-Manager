using CommunityToolkit.Mvvm.ComponentModel;
using HybridADManager.Models;

namespace HybridADManager.ViewModels;

public partial class GeneralTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public GeneralTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string DisplayName => User.DisplayName;
    public string FirstName => User.FirstName;
    public string LastName => User.LastName;
    public string Description => User.Description;
    public string Office => User.Office;
    public string Telephone => User.Telephone;
    public string Email => User.Email;
}

public partial class AccountTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public AccountTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string UserPrincipalName => User.Upn;
    public string LogonName => User.DisplayName;
    public bool AccountLockedOut => User.AccountLockedOut;
    public bool PasswordNeverExpires => User.PasswordNeverExpires;
    public bool MustChangePassword => User.MustChangePasswordAtNextLogon;
    public DateTime? AccountExpires => User.AccountExpires;
    public DateTime? PasswordLastSet => User.PasswordLastSet;
}

public partial class AddressTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public AddressTabViewModel(HybridUser user)
    {
        _user = user;
    }
}

public partial class OrganizationTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public OrganizationTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string JobTitle => User.JobTitle;
    public string Department => User.Department;
    public string Company => User.Company;
    public string Manager => User.Manager;
}

public partial class MemberOfTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _groups = new();

    public MemberOfTabViewModel(HybridUser user)
    {
        _user = user;
        LoadGroups();
    }

    private async void LoadGroups()
    {
        // TODO: Load from AD service
        Groups.Add("Domain Users");
        Groups.Add("All Users");
    }
}
