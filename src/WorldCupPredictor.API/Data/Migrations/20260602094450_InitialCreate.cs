using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BracketLockDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Season = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BracketDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    PicksJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSubmittedFinal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketDrafts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Brackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ShareToken = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brackets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brackets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    InviteCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupMembers",
                columns: table => new
                {
                    UserGroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMembers", x => new { x.UserGroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualBest3rdQualifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualBest3rdQualifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BracketBest3rdPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BracketId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketBest3rdPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketBest3rdPicks_Brackets_BracketId",
                        column: x => x.BracketId,
                        principalTable: "Brackets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BracketGroupPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BracketId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    FirstTeamId = table.Column<int>(type: "integer", nullable: true),
                    SecondTeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketGroupPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketGroupPicks_Brackets_BracketId",
                        column: x => x.BracketId,
                        principalTable: "Brackets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BracketPickLineupPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BracketPickId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketPickLineupPlayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BracketPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BracketId = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    PickedTeamId = table.Column<int>(type: "integer", nullable: true),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketPicks_Brackets_BracketId",
                        column: x => x.BracketId,
                        principalTable: "Brackets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    SlotNumber = table.Column<int>(type: "integer", nullable: true),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamId = table.Column<int>(type: "integer", nullable: true),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    MatchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalApiId = table.Column<string>(type: "text", nullable: true),
                    WinnerTeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<string>(type: "text", nullable: false),
                    ShirtNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FlagEmoji = table.Column<string>(type: "text", nullable: false),
                    FifaCode = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    Seeding = table.Column<int>(type: "integer", nullable: false),
                    FifaRanking = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TournamentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ActualFirstTeamId = table.Column<int>(type: "integer", nullable: true),
                    ActualSecondTeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentGroups_Teams_ActualFirstTeamId",
                        column: x => x.ActualFirstTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentGroups_Teams_ActualSecondTeamId",
                        column: x => x.ActualSecondTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActualBest3rdQualifiers_TeamId",
                table: "ActualBest3rdQualifiers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketBest3rdPicks_BracketId_Rank",
                table: "BracketBest3rdPicks",
                columns: new[] { "BracketId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketBest3rdPicks_TeamId",
                table: "BracketBest3rdPicks",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketDrafts_UserId",
                table: "BracketDrafts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketGroupPicks_BracketId_GroupId",
                table: "BracketGroupPicks",
                columns: new[] { "BracketId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketGroupPicks_FirstTeamId",
                table: "BracketGroupPicks",
                column: "FirstTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketGroupPicks_GroupId",
                table: "BracketGroupPicks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketGroupPicks_SecondTeamId",
                table: "BracketGroupPicks",
                column: "SecondTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketPickLineupPlayers_BracketPickId_PlayerId",
                table: "BracketPickLineupPlayers",
                columns: new[] { "BracketPickId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketPickLineupPlayers_PlayerId",
                table: "BracketPickLineupPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketPicks_BracketId_MatchId",
                table: "BracketPicks",
                columns: new[] { "BracketId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketPicks_MatchId",
                table: "BracketPicks",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BracketPicks_PickedTeamId",
                table: "BracketPicks",
                column: "PickedTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Brackets_UserId",
                table: "Brackets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_FifaCode",
                table: "Teams",
                column: "FifaCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_GroupId",
                table: "Teams",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_ActualFirstTeamId",
                table: "TournamentGroups",
                column: "ActualFirstTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_ActualSecondTeamId",
                table: "TournamentGroups",
                column: "ActualSecondTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMembers_UserId",
                table: "UserGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_InviteCode",
                table: "UserGroups",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_OwnerId",
                table: "UserGroups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActualBest3rdQualifiers_Teams_TeamId",
                table: "ActualBest3rdQualifiers",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketBest3rdPicks_Teams_TeamId",
                table: "BracketBest3rdPicks",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketGroupPicks_Teams_FirstTeamId",
                table: "BracketGroupPicks",
                column: "FirstTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketGroupPicks_Teams_SecondTeamId",
                table: "BracketGroupPicks",
                column: "SecondTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketGroupPicks_TournamentGroups_GroupId",
                table: "BracketGroupPicks",
                column: "GroupId",
                principalTable: "TournamentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketPickLineupPlayers_BracketPicks_BracketPickId",
                table: "BracketPickLineupPlayers",
                column: "BracketPickId",
                principalTable: "BracketPicks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketPickLineupPlayers_Players_PlayerId",
                table: "BracketPickLineupPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketPicks_Matches_MatchId",
                table: "BracketPicks",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BracketPicks_Teams_PickedTeamId",
                table: "BracketPicks",
                column: "PickedTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_TournamentGroups_GroupId",
                table: "Teams",
                column: "GroupId",
                principalTable: "TournamentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentGroups_Teams_ActualFirstTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentGroups_Teams_ActualSecondTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropTable(
                name: "ActualBest3rdQualifiers");

            migrationBuilder.DropTable(
                name: "BracketBest3rdPicks");

            migrationBuilder.DropTable(
                name: "BracketDrafts");

            migrationBuilder.DropTable(
                name: "BracketGroupPicks");

            migrationBuilder.DropTable(
                name: "BracketPickLineupPlayers");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TournamentConfigs");

            migrationBuilder.DropTable(
                name: "UserGroupMembers");

            migrationBuilder.DropTable(
                name: "BracketPicks");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "Brackets");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "TournamentGroups");
        }
    }
}
