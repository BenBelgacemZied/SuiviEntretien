using System.Text.Json;
using SuiviEntretien.Models;

namespace SuiviEntretien.Services;

public sealed class ApplicationStore
{
    private readonly string _file = Path.Combine(FileSystem.AppDataDirectory, "applications.json");
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<List<JobApplication>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(_file)) return new();
            await using var stream = File.OpenRead(_file);
            return await JsonSerializer.DeserializeAsync<List<JobApplication>>(stream) ?? new();
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(JobApplication item)
    {
        var items = await GetAllAsync();
        var index = items.FindIndex(x => x.Id == item.Id);
        if (index < 0) items.Add(item); else items[index] = item;
        await WriteAsync(items);
    }

    public async Task DeleteAsync(Guid id)
    {
        var items = await GetAllAsync();
        items.RemoveAll(x => x.Id == id);
        await WriteAsync(items);
    }

    private async Task WriteAsync(List<JobApplication> items)
    {
        await _gate.WaitAsync();
        try
        {
            await using var stream = File.Create(_file);
            await JsonSerializer.SerializeAsync(stream, items);
        }
        finally { _gate.Release(); }
    }
}