using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Services;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/bracket")]
[Authorize]
public class BracketController(IBracketService bracketService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMyBracket()
    {
        var bracket = await bracketService.GetBracketAsync(CurrentUserId);
        return bracket is null ? NoContent() : Ok(bracket);
    }

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetBracket(int userId)
    {
        var bracket = await bracketService.GetBracketAsync(userId);
        return bracket is null ? NotFound() : Ok(bracket);
    }

    [HttpGet("share/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedBracket(string token)
    {
        var bracket = await bracketService.GetBracketByTokenAsync(token);
        return bracket is null ? NotFound() : Ok(bracket);
    }

    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> SaveBracket([FromBody] BracketSubmitRequest request)
    {
        try
        {
            var result = await bracketService.SaveBracketAsync(CurrentUserId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
