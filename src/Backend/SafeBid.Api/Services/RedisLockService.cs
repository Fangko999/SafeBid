using RedLockNet;

namespace SafeBid.Api.Services;

public class RedisLockService
{
    private readonly IDistributedLockFactory _lockFactory;
    private int _counter = 0;

    public RedisLockService(IDistributedLockFactory lockFactory)
    {
        _lockFactory = lockFactory;
    }

    public async Task<int> IncrementCounterSafeAsync()
    {
        var resource = "spike:redlock:counter";
        var expiry = TimeSpan.FromSeconds(30);
        var wait = TimeSpan.FromSeconds(10);
        var retry = TimeSpan.FromMilliseconds(500);

        using (var redLock = await _lockFactory.CreateLockAsync(resource, expiry, wait, retry))
        {
            if (redLock.IsAcquired)
            {
                var temp = _counter;
                await Task.Delay(10); // Simulate work that could cause a race condition
                _counter = temp + 1;
                return _counter;
            }
            throw new Exception("Could not acquire lock");
        }
    }

    public int GetCounter() => _counter;
    
    public void ResetCounter() => _counter = 0;
}
