using System.Collections.Concurrent;
using Counter.Models;
using SQLite;

namespace Counter.Services;

public class CounterRepository : ICounterRepository
{
    private const int DebounceWindowMs = 150;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingSaves = new();
    private readonly ConcurrentDictionary<string, byte> _deletedTombstones = new();

    private SQLiteAsyncConnection? _database;

    public CounterRepository()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "counters_v1.db3");
    }

    private async Task InitAsync()
    {
        if (_database != null)
            return;

        await _dbLock.WaitAsync();
        try
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(
                _dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _database.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;");
            await _database.CreateTableAsync<CounterItem>();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<List<CounterItem>> HydrateAllAsync()
    {
        await InitAsync();

        var records = await _database!.Table<CounterItem>()
                                      .OrderByDescending(c => c.UpdatedAtUtc)
                                      .ToListAsync();

        foreach (var item in records)
        {
            item.ColorHex = CounterItem.NormalizeColorHex(item.ColorHex);
            item.IsSynced = true;
        }

        return records;
    }

    public async Task InsertCounterAsync(CounterItem counter)
    {
        await InitAsync();
        _deletedTombstones.TryRemove(counter.Id, out _);
        counter.ColorHex = CounterItem.NormalizeColorHex(counter.ColorHex);

        await _dbLock.WaitAsync();
        try
        {
            await _database!.InsertAsync(counter);
            counter.IsSynced = true;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public void EnqueueAutoSave(CounterItem counter)
    {
        if (_deletedTombstones.ContainsKey(counter.Id))
            return;

        if (_pendingSaves.TryRemove(counter.Id, out var previousCts))
        {
            previousCts.Cancel();
            previousCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pendingSaves[counter.Id] = cts;

        var snapshotId = counter.Id;
        var snapshotValue = counter.CurrentValue;
        var snapshotUpdatedAt = counter.UpdatedAtUtc;
        var snapshotRevision = counter.Revision;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceWindowMs, cts.Token);
                if (cts.Token.IsCancellationRequested || _deletedTombstones.ContainsKey(snapshotId))
                    return;

                await InitAsync();
                await _dbLock.WaitAsync(cts.Token);
                try
                {
                    if (_deletedTombstones.ContainsKey(snapshotId))
                        return;

                    await _database!.ExecuteAsync(
                        @"UPDATE counters 
                          SET current_value = ?, updated_at_utc = ?, is_synced = 1, revision = ? 
                          WHERE id = ? AND revision <= ?",
                        snapshotValue,
                        snapshotUpdatedAt.Ticks,
                        snapshotRevision,
                        snapshotId,
                        snapshotRevision);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (counter.Revision == snapshotRevision)
                        {
                            counter.IsSynced = true;
                        }
                    });
                }
                finally
                {
                    _dbLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_pendingSaves.TryGetValue(snapshotId, out var activeCts) && activeCts == cts)
                {
                    _pendingSaves.TryRemove(snapshotId, out _);
                }
                cts.Dispose();
            }
        });
    }

    public async Task DeleteCounterAsync(string counterId)
    {
        _deletedTombstones[counterId] = 1;

        if (_pendingSaves.TryRemove(counterId, out var pendingCts))
        {
            pendingCts.Cancel();
            pendingCts.Dispose();
        }

        await InitAsync();
        await _dbLock.WaitAsync();
        try
        {
            await _database!.ExecuteAsync("DELETE FROM counters WHERE id = ?", counterId);
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
