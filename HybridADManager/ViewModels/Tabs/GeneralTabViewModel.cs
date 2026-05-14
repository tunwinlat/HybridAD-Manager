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
    public bool CannotChangePassword => User.CannotChangePassword;
    public bool ReversibleEncryption => User.ReversibleEncryption;
    public bool SmartCardRequired => User.SmartCardRequired;
    public bool TrustedForDelegation => User.TrustedForDelegation;
    public bool SensitiveForDelegation => User.SensitiveForDelegation;
    public bool KerberosDes => User.KerberosDes;
    public bool KerberosAes128 => User.KerberosAes128;
    public bool KerberosAes256 => User.KerberosAes256;
    public bool NoPreauth => User.NoPreauth;
}

public partial class AddressTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public AddressTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string StreetAddress => User.StreetAddress;
    public string City => User.City;
    public string State => User.State;
    public string PostalCode => User.PostalCode;
    public string Country => User.Country;
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
    public System.Collections.ObjectModel.ObservableCollection<string> DirectReports => User.DirectReports;
}

public partial class MemberOfTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public MemberOfTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public System.Collections.ObjectModel.ObservableCollection<string> Groups => User.MemberOf;
}

public partial class ProfileTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public ProfileTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string ProfilePath => User.ProfilePath;
    public string LogonScript => User.LogonScript;
    public string HomeDirectory => User.HomeDirectory;
    public string HomeDrive => User.HomeDrive;
}

public partial class TelephonesTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public TelephonesTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string HomePhone => User.HomePhone;
    public string MobilePhone => User.MobilePhone;
    public string Pager => User.Pager;
    public string Fax => User.Fax;
    public string IpPhone => User.IpPhone;
    public string Notes => User.Notes;
}

public partial class DialInTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public DialInTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string DialinAccess => User.DialinAccess;
    public bool CallbackRequired => User.CallbackRequired;
    public string CallbackNumber => User.CallbackNumber;
}

public partial class EnvironmentTabViewModel : ObservableObject
{
    [ObservableProperty]
    private HybridUser _user;

    public EnvironmentTabViewModel(HybridUser user)
    {
        _user = user;
    }

    public string StartingProgram => User.StartingProgram;
    public string StartIn => User.StartIn;
    public bool ConnectClientDrives => User.ConnectClientDrives;
    public bool ConnectClientPrinters => User.ConnectClientPrinters;
    public bool DefaultToMainPrinter => User.DefaultToMainPrinter;
}
