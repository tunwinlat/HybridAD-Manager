using Microsoft.Identity.Client;

namespace HybridADManager.Services;

public interface IAuthenticationService
{
    Task<bool> IsAuthenticatedAsync();
    Task<AuthenticationResult?> AuthenticateAsync();
    Task<AuthenticationResult?> AcquireTokenSilentAsync();
    Task SignOutAsync();
    event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;
}

public class AuthenticationStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public string? AccountUsername { get; set; }
    public string? TenantId { get; set; }
}

public class AuthenticationSettings
{
    public string ClientId { get; set; } = "d3590ed6-52b3-4102-aeff-aad2292ab01c"; // Microsoft Graph Explorer client ID (for testing; replace with your own app registration)
    public string TenantId { get; set; } = "common";
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";
    public string[] Scopes { get; set; } = new[]
    {
        "User.Read.All",
        "Group.Read.All",
        "Directory.Read.All",
        "Organization.Read.All"
    };
    public string RedirectUri { get; set; } = "http://localhost";
}
