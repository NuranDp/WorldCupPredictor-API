using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScorelineToBracketPick : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayScore",
                table: "BracketPicks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeScore",
                table: "BracketPicks",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayScore",
                table: "BracketPicks");

            migrationBuilder.DropColumn(
                name: "HomeScore",
                table: "BracketPicks");
        }
    }
}
