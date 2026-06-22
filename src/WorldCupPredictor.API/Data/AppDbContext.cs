using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TournamentGroup> TournamentGroups => Set<TournamentGroup>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Bracket> Brackets => Set<Bracket>();
    public DbSet<BracketPick> BracketPicks => Set<BracketPick>();
    public DbSet<BracketGroupPick> BracketGroupPicks => Set<BracketGroupPick>();
    public DbSet<BracketBest3rdPick> BracketBest3rdPicks => Set<BracketBest3rdPick>();
    public DbSet<ActualBest3rdQualifier> ActualBest3rdQualifiers => Set<ActualBest3rdQualifier>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
    public DbSet<TournamentConfig> TournamentConfigs => Set<TournamentConfig>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<BracketPickLineupPlayer> BracketPickLineupPlayers => Set<BracketPickLineupPlayer>();
    public DbSet<BracketDraft> BracketDrafts => Set<BracketDraft>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Giveaway> Giveaways => Set<Giveaway>();
    public DbSet<GiveawayEntry> GiveawayEntries => Set<GiveawayEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e => e.HasIndex(u => u.Email).IsUnique());

        modelBuilder.Entity<Team>(e =>
        {
            e.HasIndex(t => t.FifaCode).IsUnique();
            e.HasOne(t => t.Group).WithMany(g => g.Teams)
             .HasForeignKey(t => t.GroupId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.HasOne(m => m.HomeTeam).WithMany()
             .HasForeignKey(m => m.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AwayTeam).WithMany()
             .HasForeignKey(m => m.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.WinnerTeam).WithMany()
             .HasForeignKey(m => m.WinnerTeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TournamentGroup>(e =>
        {
            e.HasOne(g => g.ActualFirstTeam).WithMany()
             .HasForeignKey(g => g.ActualFirstTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.ActualSecondTeam).WithMany()
             .HasForeignKey(g => g.ActualSecondTeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bracket>(e =>
        {
            e.HasIndex(b => b.UserId).IsUnique();
            e.HasOne(b => b.User).WithOne(u => u.Bracket)
             .HasForeignKey<Bracket>(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BracketPick>(e =>
        {
            e.HasIndex(bp => new { bp.BracketId, bp.MatchId }).IsUnique();
            e.HasOne(bp => bp.PickedTeam).WithMany()
             .HasForeignKey(bp => bp.PickedTeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BracketGroupPick>(e =>
        {
            e.HasIndex(gp => new { gp.BracketId, gp.GroupId }).IsUnique();
            e.HasOne(gp => gp.Bracket).WithMany(b => b.GroupPicks)
             .HasForeignKey(gp => gp.BracketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(gp => gp.Group).WithMany()
             .HasForeignKey(gp => gp.GroupId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(gp => gp.FirstTeam).WithMany()
             .HasForeignKey(gp => gp.FirstTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(gp => gp.SecondTeam).WithMany()
             .HasForeignKey(gp => gp.SecondTeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BracketBest3rdPick>(e =>
        {
            e.HasIndex(p => new { p.BracketId, p.Rank }).IsUnique();
            e.HasOne(p => p.Bracket).WithMany(b => b.Best3rdPicks)
             .HasForeignKey(p => p.BracketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Team).WithMany()
             .HasForeignKey(p => p.TeamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserGroup>(e =>
        {
            e.HasIndex(ug => ug.InviteCode).IsUnique();
            e.HasOne(ug => ug.Owner).WithMany(u => u.OwnedGroups)
             .HasForeignKey(ug => ug.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserGroupMember>(e =>
        {
            e.HasKey(m => new { m.UserGroupId, m.UserId });
            e.HasOne(m => m.UserGroup).WithMany(g => g.Members)
             .HasForeignKey(m => m.UserGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany(u => u.UserGroupMemberships)
             .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.HasOne(p => p.Team).WithMany()
             .HasForeignKey(p => p.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BracketPickLineupPlayer>(e =>
        {
            e.HasIndex(l => new { l.BracketPickId, l.PlayerId }).IsUnique();
            e.HasOne(l => l.BracketPick).WithMany(p => p.LineupPlayers)
             .HasForeignKey(l => l.BracketPickId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Player).WithMany()
             .HasForeignKey(l => l.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BracketDraft>(e =>
        {
            e.HasOne(d => d.User).WithMany()
             .HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            // nvarchar(max) on SQL Server, text on PostgreSQL — EF handles this automatically
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();
            e.HasOne(r => r.User).WithMany()
             .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Giveaway>(e =>
        {
            e.HasOne(g => g.Match).WithMany()
             .HasForeignKey(g => g.MatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.Winner).WithMany()
             .HasForeignKey(g => g.WinnerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GiveawayEntry>(e =>
        {
            e.HasIndex(ge => new { ge.GiveawayId, ge.UserId }).IsUnique();
            e.HasOne(ge => ge.Giveaway).WithMany(g => g.Entries)
             .HasForeignKey(ge => ge.GiveawayId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ge => ge.User).WithMany()
             .HasForeignKey(ge => ge.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
