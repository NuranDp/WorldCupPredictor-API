using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Bracket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BracketGroupPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BracketId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    FirstTeamId = table.Column<int>(type: "int", nullable: true),
                    SecondTeamId = table.Column<int>(type: "int", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_BracketGroupPicks_Teams_FirstTeamId",
                        column: x => x.FirstTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BracketGroupPicks_Teams_SecondTeamId",
                        column: x => x.SecondTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BracketGroupPicks_TournamentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BracketGroupPicks");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "Matches");
        }
    }
}
