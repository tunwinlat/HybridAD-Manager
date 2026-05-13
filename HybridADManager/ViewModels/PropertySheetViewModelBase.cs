using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridADManager.Models;
using HybridADManager.Services;

namespace HybridADManager.ViewModels;

public abstract partial class PropertySheetViewModelBase : ObservableObject
{
    [ObservableProperty]
    private DirectoryObject _directoryObject;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public PropertySheetViewModelBase(DirectoryObject directoryObject)
    {
        _directoryObject = directoryObject;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            await OnSaveAsync();
            IsDirty = false;
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
    private void Cancel()
    {
        OnCancel();
    }

    protected virtual Task OnSaveAsync() => Task.CompletedTask;
    protected virtual void OnCancel() { }
}

public partial class UserPropertySheetViewModel : PropertySheetViewModelBase
{
    [ObservableProperty]
    private GeneralTabViewModel _generalTab;

    [ObservableProperty]
    private AccountTabViewModel _accountTab;

    [ObservableProperty]
    private AddressTabViewModel _addressTab;

    [ObservableProperty]
    private OrganizationTabViewModel _organizationTab;

    [ObservableProperty]
    private MemberOfTabViewModel _memberOfTab;

    [ObservableProperty]
    private HybridStatusTabViewModel _hybridStatusTab;

    public UserPropertySheetViewModel(HybridUser user) : base(user)
    {
        var graphService = App.Current.Services.GetService(typeof(IGraphService)) as IGraphService
            ?? new GraphService(new AuthenticationService());

        GeneralTab = new GeneralTabViewModel(user);
        AccountTab = new AccountTabViewModel(user);
        AddressTab = new AddressTabViewModel(user);
        OrganizationTab = new OrganizationTabViewModel(user);
        MemberOfTab = new MemberOfTabViewModel(user);
        HybridStatusTab = new HybridStatusTabViewModel(user, graphService);
    }

    public HybridUser User => (HybridUser)DirectoryObject;
}

public partial class GroupPropertySheetViewModel : PropertySheetViewModelBase
{
    public GroupPropertySheetViewModel(HybridGroup group) : base(group) { }
    public HybridGroup Group => (HybridGroup)DirectoryObject;
}
