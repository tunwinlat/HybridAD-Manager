using HybridADManager.Models;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace HybridADManager.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    public Task<string?> GetCurrentDomainNameAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                using var domain = Domain.GetCurrentDomain();
                return domain.Name;
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<IEnumerable<DomainNode>> GetDomainsAsync()
    {
        return Task.Run(() =>
        {
            var domains = new List<DomainNode>();
            try
            {
                using var forest = Forest.GetCurrentForest();
                foreach (Domain domain in forest.Domains)
                {
                    domains.Add(new DomainNode
                    {
                        Name = domain.Name,
                        DnsName = domain.Name,
                        DistinguishedName = $"DC={domain.Name.Replace(".", ",DC=")}"
                    });
                }
            }
            catch
            {
                // Fallback to current domain only
                try
                {
                    using var domain = Domain.GetCurrentDomain();
                    domains.Add(new DomainNode
                    {
                        Name = domain.Name,
                        DnsName = domain.Name,
                        DistinguishedName = $"DC={domain.Name.Replace(".", ",DC=")}"
                    });
                }
                catch { }
            }
            return domains.AsEnumerable();
        });
    }

    public Task<IEnumerable<DirectoryNode>> GetDomainContainersAsync(string domainName)
    {
        return Task.Run(() =>
        {
            var containers = new List<DirectoryNode>();
            var rootDn = $"DC={domainName.Replace(".", ",DC=")}";

            try
            {
                using var entry = new DirectoryEntry($"LDAP://{rootDn}");
                using var searcher = new DirectorySearcher(entry);
                searcher.Filter = "(objectClass=organizationalUnit)";
                searcher.SearchScope = SearchScope.OneLevel;
                searcher.PropertiesToLoad.Add("name");
                searcher.PropertiesToLoad.Add("distinguishedName");
                searcher.PropertiesToLoad.Add("objectGUID");

                var results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    var name = result.Properties["name"]?[0]?.ToString() ?? "";
                    var dn = result.Properties["distinguishedName"]?[0]?.ToString() ?? "";

                    containers.Add(new DirectoryNode
                    {
                        DisplayName = name,
                        DistinguishedName = dn,
                        NodeType = name switch
                        {
                            "Builtin" => NodeType.BuiltinContainer,
                            "Computers" => NodeType.ComputersContainer,
                            "Domain Controllers" => NodeType.DomainControllersContainer,
                            "ForeignSecurityPrincipals" => NodeType.ForeignSecurityPrincipalsContainer,
                            "Managed Service Accounts" => NodeType.ManagedServiceAccountsContainer,
                            "Users" => NodeType.UsersContainer,
                            _ => NodeType.CustomOU
                        }
                    });
                }
            }
            catch
            {
                // Return fallback containers if AD is not accessible
                containers.AddRange(GetFallbackContainers(rootDn));
            }

            return containers.AsEnumerable();
        });
    }

    public Task<IEnumerable<DirectoryNode>> GetChildOUsAsync(string parentDistinguishedName)
    {
        return Task.Run(() =>
        {
            var children = new List<DirectoryNode>();

            try
            {
                using var entry = new DirectoryEntry($"LDAP://{parentDistinguishedName}");
                using var searcher = new DirectorySearcher(entry);
                searcher.Filter = "(objectClass=organizationalUnit)";
                searcher.SearchScope = SearchScope.OneLevel;
                searcher.PropertiesToLoad.Add("name");
                searcher.PropertiesToLoad.Add("distinguishedName");

                var results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    children.Add(new DirectoryNode
                    {
                        DisplayName = result.Properties["name"]?[0]?.ToString() ?? "",
                        DistinguishedName = result.Properties["distinguishedName"]?[0]?.ToString() ?? "",
                        NodeType = NodeType.CustomOU
                    });
                }
            }
            catch
            {
                // AD not accessible - return empty
            }

            return children.AsEnumerable();
        });
    }

    public Task<IEnumerable<DirectoryObject>> GetObjectsInContainerAsync(string containerDistinguishedName)
    {
        return Task.Run(() =>
        {
            var objects = new List<DirectoryObject>();

            try
            {
                using var entry = new DirectoryEntry($"LDAP://{containerDistinguishedName}");

                // Get Users
                objects.AddRange(SearchObjects(entry, "(&(objectClass=user)(objectCategory=person))",
                    result => CreateUserFromResult(result)));

                // Get Groups
                objects.AddRange(SearchObjects(entry, "(objectClass=group)",
                    result => CreateGroupFromResult(result)));

                // Get Computers
                objects.AddRange(SearchObjects(entry, "(objectClass=computer)",
                    result => CreateComputerFromResult(result)));
            }
            catch
            {
                // AD not accessible - return empty
            }

            return objects.AsEnumerable();
        });
    }

    public Task<HybridUser?> GetUserDetailsAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                return CreateUserFromEntry(entry);
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<HybridGroup?> GetGroupDetailsAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                return CreateGroupFromEntry(entry);
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<HybridComputer?> GetComputerDetailsAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                return CreateComputerFromEntry(entry);
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<IEnumerable<string>> GetUserGroupMembershipsAsync(string userDistinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{userDistinguishedName}");
                var memberOf = entry.Properties["memberOf"];
                var groups = new List<string>();
                foreach (var group in memberOf)
                {
                    groups.Add(group?.ToString() ?? "");
                }
                return groups.AsEnumerable();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });
    }

    public Task<bool> IsObjectSyncedToCloudAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                var msdirectory = entry.Properties["msDS-ExternalDirectoryObjectID"];
                var immutableId = entry.Properties["msDS-ConsistencyGuid"];
                return msdirectory.Count > 0 || immutableId.Count > 0;
            }
            catch
            {
                return false;
            }
        });
    }

    public Task<IEnumerable<DirectoryObject>> SearchObjectsAsync(string domainName, string? name, string? email, string? description, string? phone)
    {
        return Task.Run(() =>
        {
            var objects = new List<DirectoryObject>();
            var rootDn = $"DC={domainName.Replace(".", ",DC=")}";

            try
            {
                using var entry = new DirectoryEntry($"LDAP://{rootDn}");
                var filterParts = new List<string>();

                if (!string.IsNullOrWhiteSpace(name))
                    filterParts.Add($"(|(displayName=*{EscapeLdap(name)}*)(cn=*{EscapeLdap(name)}*)(givenName=*{EscapeLdap(name)}*)(sn=*{EscapeLdap(name)}*))");
                if (!string.IsNullOrWhiteSpace(email))
                    filterParts.Add($"(|(mail=*{EscapeLdap(email)}*)(userPrincipalName=*{EscapeLdap(email)}*))");
                if (!string.IsNullOrWhiteSpace(description))
                    filterParts.Add($"(description=*{EscapeLdap(description)}*)");
                if (!string.IsNullOrWhiteSpace(phone))
                    filterParts.Add($"(|(telephoneNumber=*{EscapeLdap(phone)}*)(mobile=*{EscapeLdap(phone)}*)(homePhone=*{EscapeLdap(phone)}*))");

                if (filterParts.Count == 0)
                    return objects.AsEnumerable();

                var filter = $"(&(|(objectClass=user)(objectClass=group)(objectClass=computer)){string.Concat(filterParts)})";

                using var searcher = new DirectorySearcher(entry);
                searcher.Filter = filter;
                searcher.SearchScope = SearchScope.Subtree;
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("distinguishedName");
                searcher.PropertiesToLoad.Add("objectGUID");
                searcher.PropertiesToLoad.Add("objectSid");
                searcher.PropertiesToLoad.Add("mail");
                searcher.PropertiesToLoad.Add("userPrincipalName");
                searcher.PropertiesToLoad.Add("description");
                searcher.PropertiesToLoad.Add("userAccountControl");
                searcher.PropertiesToLoad.Add("objectClass");
                searcher.PropertiesToLoad.Add("msDS-ExternalDirectoryObjectID");
                searcher.PropertiesToLoad.Add("msDS-ConsistencyGuid");
                searcher.PageSize = 100;

                var results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    var objectClasses = result.Properties["objectClass"];
                    if (objectClasses.Contains("user") && objectClasses.Contains("person"))
                        objects.Add(CreateUserFromResult(result));
                    else if (objectClasses.Contains("group"))
                        objects.Add(CreateGroupFromResult(result));
                    else if (objectClasses.Contains("computer"))
                        objects.Add(CreateComputerFromResult(result));
                }
            }
            catch
            {
                // AD not accessible - return empty
            }

            return objects.AsEnumerable();
        });
    }

    public Task<bool> EnableObjectAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                var uac = entry.Properties["userAccountControl"]?.Value as int? ?? 0;
                entry.Properties["userAccountControl"].Value = uac & ~0x2;
                entry.CommitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public Task<bool> DisableObjectAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                var uac = entry.Properties["userAccountControl"]?.Value as int? ?? 0;
                entry.Properties["userAccountControl"].Value = uac | 0x2;
                entry.CommitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public Task<bool> DeleteObjectAsync(string distinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                entry.DeleteTree();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public Task<bool> MoveObjectAsync(string distinguishedName, string targetContainerDistinguishedName)
    {
        return Task.Run(() =>
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{distinguishedName}");
                using var target = new DirectoryEntry($"LDAP://{targetContainerDistinguishedName}");
                var newName = entry.Properties["name"]?.Value?.ToString() ?? "CN=Unknown";
                entry.MoveTo(target, newName);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    private static string EscapeLdap(string input)
    {
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00")
            .Replace("/", "\\2f");
    }

    // Helper methods
    private IEnumerable<DirectoryObject> SearchObjects(DirectoryEntry searchRoot, string filter,
        Func<SearchResult, DirectoryObject> factory)
    {
        using var searcher = new DirectorySearcher(searchRoot);
        searcher.Filter = filter;
        searcher.SearchScope = SearchScope.OneLevel;
        searcher.PropertiesToLoad.Add("displayName");
        searcher.PropertiesToLoad.Add("distinguishedName");
        searcher.PropertiesToLoad.Add("objectGUID");
        searcher.PropertiesToLoad.Add("objectSid");
        searcher.PropertiesToLoad.Add("mail");
        searcher.PropertiesToLoad.Add("userPrincipalName");
        searcher.PropertiesToLoad.Add("description");
        searcher.PropertiesToLoad.Add("userAccountControl");
        searcher.PropertiesToLoad.Add("msDS-ExternalDirectoryObjectID");
        searcher.PropertiesToLoad.Add("msDS-ConsistencyGuid");

        var results = searcher.FindAll();
        foreach (SearchResult result in results)
        {
            yield return factory(result);
        }
    }

    private HybridUser CreateUserFromResult(SearchResult result)
    {
        var user = new HybridUser
        {
            DisplayName = GetProperty(result, "displayName"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            ObjectGuid = GetGuid(result, "objectGUID"),
            ObjectSid = GetSid(result, "objectSid"),
            Email = GetProperty(result, "mail"),
            Upn = GetProperty(result, "userPrincipalName"),
            Description = GetProperty(result, "description"),
            IsEnabled = !IsAccountDisabled(result),
            FirstName = GetProperty(result, "givenName"),
            LastName = GetProperty(result, "sn"),
            Office = GetProperty(result, "physicalDeliveryOfficeName"),
            Telephone = GetProperty(result, "telephoneNumber"),
            Department = GetProperty(result, "department"),
            Company = GetProperty(result, "company"),
            JobTitle = GetProperty(result, "title"),
            Manager = GetProperty(result, "manager"),
            EntraObjectId = GetProperty(result, "msDS-ExternalDirectoryObjectID"),
            ImmutableId = GetProperty(result, "msDS-ConsistencyGuid"),
            SyncStatus = new SyncStatus
            {
                Status = GetSyncStatus(result)
            }
        };

        return user;
    }

    private HybridUser? CreateUserFromEntry(DirectoryEntry entry)
    {
        try
        {
            return new HybridUser
            {
                DisplayName = entry.Properties["displayName"]?.Value?.ToString() ?? "",
                DistinguishedName = entry.Properties["distinguishedName"]?.Value?.ToString() ?? "",
                ObjectGuid = entry.Properties["objectGUID"]?.Value != null
                    ? new Guid((byte[])entry.Properties["objectGUID"].Value!).ToString()
                    : "",
                Email = entry.Properties["mail"]?.Value?.ToString() ?? "",
                Upn = entry.Properties["userPrincipalName"]?.Value?.ToString() ?? "",
                Description = entry.Properties["description"]?.Value?.ToString() ?? "",
                FirstName = entry.Properties["givenName"]?.Value?.ToString() ?? "",
                LastName = entry.Properties["sn"]?.Value?.ToString() ?? "",
                Office = entry.Properties["physicalDeliveryOfficeName"]?.Value?.ToString() ?? "",
                Telephone = entry.Properties["telephoneNumber"]?.Value?.ToString() ?? "",
                Department = entry.Properties["department"]?.Value?.ToString() ?? "",
                Company = entry.Properties["company"]?.Value?.ToString() ?? "",
                JobTitle = entry.Properties["title"]?.Value?.ToString() ?? "",
                Manager = entry.Properties["manager"]?.Value?.ToString() ?? "",
                AccountExpires = entry.Properties["accountExpires"]?.Value != null
                    ? DateTime.FromFileTime((long)entry.Properties["accountExpires"].Value!)
                    : null,
                PasswordLastSet = entry.Properties["pwdLastSet"]?.Value != null
                    ? DateTime.FromFileTime((long)entry.Properties["pwdLastSet"].Value!)
                    : null,
                PasswordNeverExpires = (GetUacValue(entry) & 0x10000) != 0,
                MustChangePasswordAtNextLogon = GetUacValue(entry) == 0,
                AccountLockedOut = entry.Properties["lockoutTime"]?.Value != null
                    && (long)entry.Properties["lockoutTime"].Value! > 0,
                EntraObjectId = entry.Properties["msDS-ExternalDirectoryObjectID"]?.Value?.ToString(),
                ImmutableId = entry.Properties["msDS-ConsistencyGuid"]?.Value != null
                    ? Convert.ToBase64String((byte[])entry.Properties["msDS-ConsistencyGuid"].Value!)
                    : null,
                SyncStatus = new SyncStatus
                {
                    Status = GetSyncStatusFromEntry(entry)
                }
            };
        }
        catch
        {
            return null;
        }
    }

    private HybridGroup CreateGroupFromResult(SearchResult result)
    {
        return new HybridGroup
        {
            DisplayName = GetProperty(result, "displayName") ?? GetProperty(result, "name"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            ObjectGuid = GetGuid(result, "objectGUID"),
            Description = GetProperty(result, "description"),
            Email = GetProperty(result, "mail"),
            GroupScope = GetGroupScope(result),
            GroupType = GetGroupType(result),
            MailEnabled = !string.IsNullOrEmpty(GetProperty(result, "mail")),
            SyncStatus = new SyncStatus
            {
                Status = GetSyncStatus(result)
            }
        };
    }

    private HybridGroup? CreateGroupFromEntry(DirectoryEntry entry)
    {
        try
        {
            var groupTypeValue = entry.Properties["groupType"]?.Value as int? ?? 0;
            return new HybridGroup
            {
                DisplayName = entry.Properties["displayName"]?.Value?.ToString()
                    ?? entry.Properties["name"]?.Value?.ToString() ?? "",
                DistinguishedName = entry.Properties["distinguishedName"]?.Value?.ToString() ?? "",
                Description = entry.Properties["description"]?.Value?.ToString() ?? "",
                Email = entry.Properties["mail"]?.Value?.ToString() ?? "",
                GroupScope = (groupTypeValue & 0x00000002) != 0 ? "Global"
                    : (groupTypeValue & 0x00000004) != 0 ? "Domain Local"
                    : (groupTypeValue & 0x00000008) != 0 ? "Universal" : "Unknown",
                GroupType = (groupTypeValue & 0x80000000) != 0 ? "Security" : "Distribution",
                MailEnabled = !string.IsNullOrEmpty(entry.Properties["mail"]?.Value?.ToString()),
                SyncStatus = new SyncStatus
                {
                    Status = GetSyncStatusFromEntry(entry)
                }
            };
        }
        catch
        {
            return null;
        }
    }

    private HybridComputer CreateComputerFromResult(SearchResult result)
    {
        return new HybridComputer
        {
            DisplayName = GetProperty(result, "name"),
            DistinguishedName = GetProperty(result, "distinguishedName"),
            ObjectGuid = GetGuid(result, "objectGUID"),
            DnsHostName = GetProperty(result, "dNSHostName"),
            OperatingSystem = GetProperty(result, "operatingSystem"),
            OperatingSystemVersion = GetProperty(result, "operatingSystemVersion"),
            Description = GetProperty(result, "description"),
            SyncStatus = new SyncStatus
            {
                Status = GetSyncStatus(result)
            }
        };
    }

    private HybridComputer? CreateComputerFromEntry(DirectoryEntry entry)
    {
        try
        {
            return new HybridComputer
            {
                DisplayName = entry.Properties["name"]?.Value?.ToString() ?? "",
                DistinguishedName = entry.Properties["distinguishedName"]?.Value?.ToString() ?? "",
                DnsHostName = entry.Properties["dNSHostName"]?.Value?.ToString() ?? "",
                OperatingSystem = entry.Properties["operatingSystem"]?.Value?.ToString() ?? "",
                OperatingSystemVersion = entry.Properties["operatingSystemVersion"]?.Value?.ToString() ?? "",
                Description = entry.Properties["description"]?.Value?.ToString() ?? "",
                LastLogonDate = entry.Properties["lastLogonTimestamp"]?.Value != null
                    ? DateTime.FromFileTime((long)entry.Properties["lastLogonTimestamp"].Value!)
                    : null,
                SyncStatus = new SyncStatus
                {
                    Status = GetSyncStatusFromEntry(entry)
                }
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetProperty(SearchResult result, string propertyName)
    {
        return result.Properties[propertyName]?[0]?.ToString() ?? "";
    }

    private static string GetGuid(SearchResult result, string propertyName)
    {
        var value = result.Properties[propertyName]?[0];
        return value != null ? new Guid((byte[])value).ToString() : "";
    }

    private static string GetSid(SearchResult result, string propertyName)
    {
        var value = result.Properties[propertyName]?[0];
        return value != null ? new System.Security.Principal.SecurityIdentifier((byte[])value, 0).ToString() : "";
    }

    private static bool IsAccountDisabled(SearchResult result)
    {
        var uac = result.Properties["userAccountControl"]?.[0] as int? ?? 0;
        return (uac & 0x2) != 0;
    }

    private static int GetUacValue(DirectoryEntry entry)
    {
        return entry.Properties["userAccountControl"]?.Value as int? ?? 0;
    }

    private static SyncState GetSyncStatus(SearchResult result)
    {
        var entraId = result.Properties["msDS-ExternalDirectoryObjectID"]?.Count > 0;
        var immutableId = result.Properties["msDS-ConsistencyGuid"]?.Count > 0;

        if (entraId || immutableId)
            return SyncState.InSync;

        return SyncState.Unknown;
    }

    private static SyncState GetSyncStatusFromEntry(DirectoryEntry entry)
    {
        var entraId = entry.Properties["msDS-ExternalDirectoryObjectID"]?.Count > 0;
        var immutableId = entry.Properties["msDS-ConsistencyGuid"]?.Count > 0;

        if (entraId || immutableId)
            return SyncState.InSync;

        return SyncState.Unknown;
    }

    private static string GetGroupScope(SearchResult result)
    {
        var groupType = result.Properties["groupType"]?.[0] as int? ?? 0;
        return (groupType & 0x00000002) != 0 ? "Global"
            : (groupType & 0x00000004) != 0 ? "Domain Local"
            : (groupType & 0x00000008) != 0 ? "Universal" : "Unknown";
    }

    private static string GetGroupType(SearchResult result)
    {
        var groupType = result.Properties["groupType"]?.[0] as int? ?? 0;
        return (groupType & 0x80000000) != 0 ? "Security" : "Distribution";
    }

    private static IEnumerable<DirectoryNode> GetFallbackContainers(string rootDn)
    {
        return new[]
        {
            new DirectoryNode { DisplayName = "Builtin", DistinguishedName = $"CN=Builtin,{rootDn}", NodeType = NodeType.BuiltinContainer },
            new DirectoryNode { DisplayName = "Computers", DistinguishedName = $"CN=Computers,{rootDn}", NodeType = NodeType.ComputersContainer },
            new DirectoryNode { DisplayName = "Domain Controllers", DistinguishedName = $"OU=Domain Controllers,{rootDn}", NodeType = NodeType.DomainControllersContainer },
            new DirectoryNode { DisplayName = "ForeignSecurityPrincipals", DistinguishedName = $"CN=ForeignSecurityPrincipals,{rootDn}", NodeType = NodeType.ForeignSecurityPrincipalsContainer },
            new DirectoryNode { DisplayName = "Managed Service Accounts", DistinguishedName = $"CN=Managed Service Accounts,{rootDn}", NodeType = NodeType.ManagedServiceAccountsContainer },
            new DirectoryNode { DisplayName = "Users", DistinguishedName = $"CN=Users,{rootDn}", NodeType = NodeType.UsersContainer }
        };
    }
}
