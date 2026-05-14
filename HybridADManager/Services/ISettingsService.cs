using HybridADManager.Models;

namespace HybridADManager.Services;

public interface ISettingsService
{
    Task<IEnumerable<SavedQuery>> GetSavedQueriesAsync();
    Task SaveSavedQueryAsync(SavedQuery query);
    Task DeleteSavedQueryAsync(string queryId);
}
