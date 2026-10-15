namespace WindowsISODownloader;
internal sealed class TimedMetadataCache
{
    private readonly Dictionary<string, (DateTime Until, Task<object> Value)> _entries = new();
    private readonly TimeSpan _ttl;
    private readonly Func<DateTime> _clock;
    internal TimedMetadataCache(TimeSpan? ttl = null, Func<DateTime>? clock = null) { _ttl = ttl ?? TimeSpan.FromMinutes(5); _clock = clock ?? (() => DateTime.UtcNow); }
    internal async Task<T> GetAsync<T>(string key, Func<Task<T>> fetch, CancellationToken token)
    {
        Task<object> task;
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out var entry) || entry.Until < _clock())
            {
                task = FetchBoxed();
                _entries[key] = (_clock().Add(_ttl), task);
            }
            else task = entry.Value;
        }
        try { return (T)await task.WaitAsync(token); }
        catch { if (task.IsFaulted || task.IsCanceled) lock (_entries) { if (_entries.TryGetValue(key, out var entry) && ReferenceEquals(entry.Value, task)) _entries.Remove(key); } throw; }
        async Task<object> FetchBoxed() => (object)(await fetch())!;
    }

}
