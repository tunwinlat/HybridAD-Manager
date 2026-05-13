using CommunityToolkit.Mvvm.ComponentModel;

namespace HybridADManager.Models;

public partial class DirectoryObject : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _distinguishedName = string.Empty;

    [ObservableProperty]
    private string _objectClass = string.Empty;

    [ObservableProperty]
    private string _objectGuid = string.Empty;

    [ObservableProperty]
    private string _objectSid = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _upn = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private SyncStatus _syncStatus = new();

    [ObservableProperty]
    private string? _entraObjectId;

    [ObservableProperty]
    private string? _immutableId;

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    public virtual string ObjectType => "Unknown";
}

public partial class HybridUser : DirectoryObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _office = string.Empty;

    [ObservableProperty]
    private string _telephone = string.Empty;

    [ObservableProperty]
    private string _department = string.Empty;

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _jobTitle = string.Empty;

    [ObservableProperty]
    private string _manager = string.Empty;

    [ObservableProperty]
    private DateTime? _accountExpires;

    [ObservableProperty]
    private DateTime? _passwordLastSet;

    [ObservableProperty]
    private bool _passwordNeverExpires;

    [ObservableProperty]
    private bool _mustChangePasswordAtNextLogon;

    [ObservableProperty]
    private bool _accountLockedOut;

    public override string ObjectType => "User";
}

public partial class HybridGroup : DirectoryObject
{
    [ObservableProperty]
    private string _groupScope = string.Empty;

    [ObservableProperty]
    private string _groupType = string.Empty;

    [ObservableProperty]
    private int _memberCount;

    [ObservableProperty]
    private bool _mailEnabled;

    public override string ObjectType => "Group";
}

public partial class HybridComputer : DirectoryObject
{
    [ObservableProperty]
    private string _dnsHostName = string.Empty;

    [ObservableProperty]
    private string _operatingSystem = string.Empty;

    [ObservableProperty]
    private string _operatingSystemVersion = string.Empty;

    [ObservableProperty]
    private DateTime? _lastLogonDate;

    public override string ObjectType => "Computer";
}

public partial class OrganizationalUnit : DirectoryObject
{
    public override string ObjectType => "Organizational Unit";
}
