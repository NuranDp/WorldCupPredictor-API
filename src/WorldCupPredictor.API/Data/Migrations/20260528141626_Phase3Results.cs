using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Results : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualFirstTeamId",
                table: "TournamentGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualSecondTeamId",
                table: "TournamentGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinnerTeamId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActualBest3rdQualifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualBest3rdQualifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualBest3rdQualifiers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_ActualFirstTeamId",
                table: "TournamentGroups",
                column: "ActualFirstTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_ActualSecondTeamId",
                table: "TournamentGroups",
                column: "ActualSecondTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualBest3rdQualifiers_TeamId",
                table: "ActualBest3rdQualifiers",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentGroups_Teams_ActualFirstTeamId",
                table: "TournamentGroups",
                column: "ActualFirstTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentGroups_Teams_ActualSecondTeamId",
                table: "TournamentGroups",
                column: "ActualSecondTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_WinnerTeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentGroups_Teams_ActualFirstTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentGroups_Teams_ActualSecondTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropTable(
                name: "ActualBest3rdQualifiers");

            migrationBuilder.DropIndex(
                name: "IX_TournamentGroups_ActualFirstTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropIndex(
                name: "IX_TournamentGroups_ActualSecondTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ActualFirstTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropColumn(
                name: "ActualSecondTeamId",
                table: "TournamentGroups");

            migrationBuilder.DropColumn(
                name: "WinnerTeamId",
                table: "Matches");
        }
    }
}
