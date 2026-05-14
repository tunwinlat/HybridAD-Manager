using HybridADManager.Models;
using System.Text.Json;

namespace HybridADManager.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _savedQueriesPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SettingsService()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HybridADManager");
        _savedQueriesPath = Path.Combine(_settingsDirectory, "savedQueries.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IEnumerable<SavedQuery>> GetSavedQueriesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_savedQueriesPath))
                return Enumerable.Empty<SavedQuery>();

            var json = await File.ReadAllTextAsync(_savedQueriesPath);
            var queries = JsonSerializer.Deserialize<List<SavedQuery>>(json, _jsonOptions);
            return queries ?? Enumerable.Empty<SavedQuery>();
        }
        catch
        {
            return Enumerable.Empty<SavedQuery>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSavedQueryAsync(SavedQuery query)
    {
        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var queries = (await GetSavedQueriesAsync()).ToList();

            var existing = queries.FirstOrDefault(q => q.Id == query.Id);
            if (existing != null)
            {
                queries.Remove(existing);
            }
            queries.Add(query);

            var json = JsonSerializer.Serialize(queries, _jsonOptions);
            await File.WriteAllTextAsync(_savedQueriesPath, json);
        }
        catch
        {
            // Silently fail to keep UI functional
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteSavedQueryAsync(string queryId)
    {
        await _lock.WaitAsync();
        try
        {
            var queries = (await GetSavedQueriesAsync()).ToList();
            queries.RemoveAll(q => q.Id == queryId);

            Directory.CreateDirectory(_settingsDirectory);
            var json = JsonSerializer.Serialize(queries, _jsonOptions);
            await File.WriteAllTextAsync(_savedQueriesPath, json);
        }
        catch
        {
            // Silently fail
        }
        finally
        {
            _lock.Release();
        }
    }
}
