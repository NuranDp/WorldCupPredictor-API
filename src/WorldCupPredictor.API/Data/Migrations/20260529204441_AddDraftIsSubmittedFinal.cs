using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupPredictor.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftIsSubmittedFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubmittedFinal",
                table: "BracketDrafts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubmittedFinal",
                table: "BracketDrafts");
        }
    }
}
