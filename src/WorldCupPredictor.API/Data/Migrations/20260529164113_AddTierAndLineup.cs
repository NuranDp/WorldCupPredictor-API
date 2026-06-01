using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTierAndLineup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Brackets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BracketPickLineupPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BracketPickId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BracketPickLineupPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BracketPickLineupPlayers_BracketPicks_BracketPickId",
                        column: x => x.BracketPickId,
                        principalTable: "BracketPicks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BracketPickLineupPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BracketPickLineupPlayers_BracketPickId_PlayerId",
                table: "BracketPickLineupPlayers",
                columns: new[] { "BracketPickId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketPickLineupPlayers_PlayerId",
                table: "BracketPickLineupPlayers",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BracketPickLineupPlayers");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Brackets");
        }
    }
}
