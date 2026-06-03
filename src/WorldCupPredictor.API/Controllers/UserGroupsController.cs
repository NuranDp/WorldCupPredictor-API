using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class UserGroupsController(AppDbContext db) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")!);

    // ── List my groups ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = CurrentUserId;

        var groups = await db.UserGroups
            .Include(g => g.Owner)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Where(g => g.OwnerId == userId
                     || g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.InviteCode,
                g.CreatedAt,
                OwnerId = g.OwnerId,
                OwnerName = g.Owner.Name,
                IsOwner = g.OwnerId == userId,
                MemberCount = g.Members.Count,
            })
            .ToListAsync();

        return Ok(groups);
    }

    // ── Create a group ────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Group name is required." });

        var userId = CurrentUserId;
        var inviteCode = GenerateCode();

        // Ensure uniqueness
        while (await db.UserGroups.AnyAsync(g => g.InviteCode == inviteCode))
            inviteCode = GenerateCode();

        var group = new UserGroup
        {
            Name = req.Name.Trim(),
            OwnerId = userId,
            InviteCode = inviteCode,
        };
        db.UserGroups.Add(group);

        // Owner is automatically a member
        var member = new UserGroupMember { UserGroup = group, UserId = userId };
        db.UserGroupMembers.Add(member);

        await db.SaveChangesAsync();

        return Ok(new
        {
            group.Id,
            group.Name,
            group.InviteCode,
            MemberCount = 1,
            IsOwner = true,
        });
    }

    // ── Join by invite code ───────────────────────────────────────────────────
    [HttpPost("join")]
    public async Task<IActionResult> JoinGroup([FromBody] JoinGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.InviteCode))
            return BadRequest(new { message = "Invite code is required." });

        var userId = CurrentUserId;
        var group = await db.UserGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.InviteCode == req.InviteCode.Trim().ToUpper());

        if (group is null)
            return NotFound(new { message = "Invalid invite code." });

        if (group.Members.Any(m => m.UserId == userId))
            return Conflict(new { message = "You are already a member of this group." });

        db.UserGroupMembers.Add(new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
        });

        await db.SaveChangesAsync();

        return Ok(new
        {
            group.Id,
            group.Name,
            group.InviteCode,
            MemberCount = group.Members.Count + 1,
            IsOwner = group.OwnerId == userId,
        });
    }

    // ── Group leaderboard ─────────────────────────────────────────────────────
    [HttpGet("{id:int}/leaderboard")]
    public async Task<IActionResult> GetGroupLeaderboard(int id, [FromQuery] string? tier = null)
    {
        var userId = CurrentUserId;

        // Must be a member
        var isMember = await db.UserGroupMembers
            .AnyAsync(m => m.UserGroupId == id && m.UserId == userId);
        if (!isMember)
            return Forbid();

        var group = await db.UserGroups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
                    .ThenInclude(u => u.Bracket)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null) return NotFound();

        BracketTier? tierFilter = null;
        if (!string.IsNullOrWhiteSpace(tier) && Enum.TryParse<BracketTier>(tier, ignoreCase: true, out var parsedTier))
            tierFilter = parsedTier;

        var entries = group.Members
            .Where(m => m.User.Bracket is not null
                     && (!tierFilter.HasValue || m.User.Bracket.Tier == tierFilter.Value))
            .Select(m => new
            {
                UserId      = m.UserId,
                Name        = m.User.Name,
                AvatarUrl   = m.User.AvatarUrl,
                TotalPoints = m.User.Bracket!.TotalPoints,
                SubmittedAt = m.User.Bracket.SubmittedAt,
            })
            .OrderByDescending(e => e.TotalPoints)
            .ThenBy(e => e.SubmittedAt)
            .Select((e, i) => new
            {
                Rank = i + 1,
                e.UserId,
                e.Name,
                e.AvatarUrl,
                e.TotalPoints,
                e.SubmittedAt,
            })
            .ToList();

        return Ok(new
        {
            group.Id,
            group.Name,
            group.InviteCode,
            OwnerId = group.OwnerId,
            Entries = entries,
        });
    }

    // ── Leave a group ─────────────────────────────────────────────────────────
    [HttpDelete("{id:int}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        var userId = CurrentUserId;

        var group = await db.UserGroups.FindAsync(id);
        if (group is null) return NotFound();

        if (group.OwnerId == userId)
            return BadRequest(new { message = "Owner cannot leave. Delete the group instead." });

        var membership = await db.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == id && m.UserId == userId);

        if (membership is null) return NotFound();

        db.UserGroupMembers.Remove(membership);
        await db.SaveChangesAsync();

        return Ok(new { message = "Left group." });
    }

    // ── Delete a group (owner only) ───────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var userId = CurrentUserId;

        var group = await db.UserGroups.FindAsync(id);
        if (group is null) return NotFound();

        if (group.OwnerId != userId) return Forbid();

        db.UserGroups.Remove(group);
        await db.SaveChangesAsync();

        return Ok(new { message = "Group deleted." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string GenerateCode() =>
        Guid.NewGuid().ToString("N")[..10].ToUpper();
}

public record CreateGroupRequest(string Name);
public record JoinGroupRequest(string InviteCode);
