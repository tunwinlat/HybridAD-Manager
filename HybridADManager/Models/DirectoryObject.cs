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

    [ObservableProperty]
    private string _streetAddress = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private string _postalCode = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private string _profilePath = string.Empty;

    [ObservableProperty]
    private string _logonScript = string.Empty;

    [ObservableProperty]
    private string _homeDirectory = string.Empty;

    [ObservableProperty]
    private string _homeDrive = string.Empty;

    [ObservableProperty]
    private string _homePhone = string.Empty;

    [ObservableProperty]
    private string _mobilePhone = string.Empty;

    [ObservableProperty]
    private string _pager = string.Empty;

    [ObservableProperty]
    private string _fax = string.Empty;

    [ObservableProperty]
    private string _ipPhone = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _cannotChangePassword;

    [ObservableProperty]
    private bool _reversibleEncryption;

    [ObservableProperty]
    private bool _smartCardRequired;

    [ObservableProperty]
    private bool _trustedForDelegation;

    [ObservableProperty]
    private bool _sensitiveForDelegation;

    [ObservableProperty]
    private bool _kerberosDes;

    [ObservableProperty]
    private bool _kerberosAes128;

    [ObservableProperty]
    private bool _kerberosAes256;

    [ObservableProperty]
    private bool _noPreauth;

    [ObservableProperty]
    private string _dialinAccess = "Control access through NPS Network Policy";

    [ObservableProperty]
    private bool _callbackRequired;

    [ObservableProperty]
    private string _callbackNumber = string.Empty;

    [ObservableProperty]
    private string _startingProgram = string.Empty;

    [ObservableProperty]
    private string _startIn = string.Empty;

    [ObservableProperty]
    private bool _connectClientDrives;

    [ObservableProperty]
    private bool _connectClientPrinters;

    [ObservableProperty]
    private bool _defaultToMainPrinter;

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _proxyAddresses = new();

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _directReports = new();

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _memberOf = new();

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
