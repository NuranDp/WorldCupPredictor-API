using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBest3rdPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BracketBest3rdPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BracketId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_BracketBest3rdPicks_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BracketBest3rdPicks_BracketId_Rank",
                table: "BracketBest3rdPicks",
                columns: new[] { "BracketId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BracketBest3rdPicks_TeamId",
                table: "BracketBest3rdPicks",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BracketBest3rdPicks");
        }
    }
}
