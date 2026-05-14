using HybridADManager.Models;

namespace HybridADManager.Services;

public interface IActiveDirectoryService
{
    Task<string?> GetCurrentDomainNameAsync();
    Task<IEnumerable<DomainNode>> GetDomainsAsync();
    Task<IEnumerable<DirectoryNode>> GetDomainContainersAsync(string domainName);
    Task<IEnumerable<DirectoryNode>> GetChildOUsAsync(string parentDistinguishedName);
    Task<IEnumerable<DirectoryObject>> GetObjectsInContainerAsync(string containerDistinguishedName);
    Task<HybridUser?> GetUserDetailsAsync(string distinguishedName);
    Task<HybridGroup?> GetGroupDetailsAsync(string distinguishedName);
    Task<HybridComputer?> GetComputerDetailsAsync(string distinguishedName);
    Task<IEnumerable<string>> GetUserGroupMembershipsAsync(string userDistinguishedName);
    Task<bool> IsObjectSyncedToCloudAsync(string distinguishedName);
    Task<IEnumerable<DirectoryObject>> SearchObjectsAsync(string domainName, string? name, string? email, string? description, string? phone);
    Task<bool> EnableObjectAsync(string distinguishedName);
    Task<bool> DisableObjectAsync(string distinguishedName);
    Task<bool> DeleteObjectAsync(string distinguishedName);
    Task<bool> MoveObjectAsync(string distinguishedName, string targetContainerDistinguishedName);
}

public class DomainNode
{
    public string Name { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string DnsName { get; set; } = string.Empty;
}
