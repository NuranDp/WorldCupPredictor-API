using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupPredictor.API.Services;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController(IPushService pushService, IConfiguration config) : ControllerBase
{
    private int? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return raw is not null && int.TryParse(raw, out var id) ? id : null;
        }
    }

    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetPublicKey()
    {
        var key = config["Vapid:PublicKey"];
        if (string.IsNullOrEmpty(key)) return NotFound();
        return Ok(new { publicKey = key });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest req)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        await pushService.SubscribeAsync(userId.Value, req.Endpoint, req.P256dh, req.Auth);
        return Ok(new { message = "Subscribed." });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest req)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        await pushService.UnsubscribeAsync(userId.Value, req.Endpoint);
        return Ok(new { message = "Unsubscribed." });
    }
}

public record PushSubscribeRequest(string Endpoint, string P256dh, string Auth);
public record PushUnsubscribeRequest(string Endpoint);
