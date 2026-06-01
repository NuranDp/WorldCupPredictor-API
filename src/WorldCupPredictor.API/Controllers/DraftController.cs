using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Services;
using System.Security.Claims;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/drafts")]
[Authorize]
public class DraftController(IDraftService draftService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await draftService.ListDraftsAsync(UserId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var draft = await draftService.GetDraftAsync(UserId, id);
        return draft is null ? NotFound() : Ok(draft);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveDraftRequest request)
    {
        try
        {
            var meta = await draftService.SaveDraftAsync(UserId, request);
            return Ok(meta);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await draftService.DeleteDraftAsync(UserId, id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            var bracket = await draftService.SubmitDraftAsync(UserId, id);
            return Ok(bracket);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
