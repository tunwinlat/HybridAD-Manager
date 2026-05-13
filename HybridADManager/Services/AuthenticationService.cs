using Microsoft.Identity.Client;
using System.IO;
using System.Security.Cryptography;

namespace HybridADManager.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IPublicClientApplication _pca;
    private readonly AuthenticationSettings _settings;
    private IAccount? _currentAccount;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    public AuthenticationService(AuthenticationSettings? settings = null)
    {
        _settings = settings ?? new AuthenticationSettings();

        var storageProperties = new StorageCreationPropertiesBuilder(
            "HybridADManager.cache",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HybridADManager"))
            .WithLinuxKeyring(
                "com.hybridadmanager.tokencache",
                "default",
                "HybridAD-Manager Token Cache")
            .WithMacKeychain(
                "com.hybridadmanager.tokencache",
                "HybridAD-Manager Token Cache")
            .Build();

        _pca = PublicClientApplicationBuilder
            .Create(_settings.ClientId)
            .WithAuthority(_settings.Authority)
            .WithRedirectUri(_settings.RedirectUri)
            .WithDefaultRedirectUri()
            .Build();

        // Register token cache helper (Windows DPAPI protected)
        var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();
        cacheHelper.RegisterCache(_pca.UserTokenCache);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        _currentAccount = accounts.FirstOrDefault();
        return _currentAccount != null;
    }

    public async Task<AuthenticationResult?> AuthenticateAsync()
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync();
            _currentAccount = accounts.FirstOrDefault();

            if (_currentAccount != null)
            {
                try
                {
                    var result = await _pca.AcquireTokenSilent(_settings.Scopes, _currentAccount)
                        .ExecuteAsync();
                    OnAuthenticated(result);
                    return result;
                }
                catch (MsalUiRequiredException)
                {
                    // Silent failed, fall through to interactive
                }
            }

            var interactiveResult = await _pca.AcquireTokenInteractive(_settings.Scopes)
                .WithParentActivityOrWindow(GetWindowHandle())
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            _currentAccount = interactiveResult.Account;
            OnAuthenticated(interactiveResult);
            return interactiveResult;
        }
        catch (MsalException ex)
        {
            // Authentication failed
            System.Diagnostics.Debug.WriteLine($"MSAL Error: {ex.Message}");
            return null;
        }
    }

    public async Task<AuthenticationResult?> AcquireTokenSilentAsync()
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync();
            _currentAccount = accounts.FirstOrDefault();

            if (_currentAccount == null)
                return null;

            var result = await _pca.AcquireTokenSilent(_settings.Scopes, _currentAccount)
                .ExecuteAsync();

            return result;
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
        catch (MsalException)
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        var accounts = await _pca.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _pca.RemoveAsync(account);
        }

        _currentAccount = null;
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
        {
            IsAuthenticated = false,
            AccountUsername = null,
            TenantId = null
        });
    }

    private void OnAuthenticated(AuthenticationResult result)
    {
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
        {
            IsAuthenticated = true,
            AccountUsername = result.Account?.Username,
            TenantId = result.TenantId
        });
    }

    private static IntPtr GetWindowHandle()
    {
        // Get the main window handle for parent window
        if (System.Windows.Application.Current?.MainWindow != null)
        {
            return new System.Windows.Interop.WindowInteropHelper(
                System.Windows.Application.Current.MainWindow).Handle;
        }
        return IntPtr.Zero;
    }
}
