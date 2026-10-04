using Microsoft.AspNetCore.Mvc;
using SafeBid.Api.Services;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/spikes/redlock")]
public class SpikeRedLockController : ControllerBase
{
    private readonly RedisLockService _redisLockService;

    public SpikeRedLockController(RedisLockService redisLockService)
    {
        _redisLockService = redisLockService;
    }

    [HttpPost]
    public async Task<IActionResult> Increment()
    {
        try
        {
            var count = await _redisLockService.IncrementCounterSafeAsync();
            return Ok(new { count = count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult Get()
    {
        var count = _redisLockService.GetCounter();
        return Ok(new { count = count });
    }

    [HttpDelete]
    public IActionResult Reset()
    {
        _redisLockService.ResetCounter();
        return Ok(new { message = "Reset" });
    }
}
