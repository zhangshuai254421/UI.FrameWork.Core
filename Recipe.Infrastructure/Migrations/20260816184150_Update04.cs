using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PressureParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Data unique identifier.")
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetPressure = table.Column<double>(type: "REAL", nullable: false),
                    MinTolerance = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PressureParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PressureParameter_Recipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PressureParameter_RecipeId",
                table: "PressureParameter",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PressureParameter");
        }
    }
}
