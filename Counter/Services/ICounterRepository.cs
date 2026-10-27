using Counter.Models;

namespace Counter.Services;

public interface ICounterRepository
{
    Task<List<CounterItem>> HydrateAllAsync();
    Task InsertCounterAsync(CounterItem counter);
    void EnqueueAutoSave(CounterItem counter);
    Task DeleteCounterAsync(string counterId);
}
