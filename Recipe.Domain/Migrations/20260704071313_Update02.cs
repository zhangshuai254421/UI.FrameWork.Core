using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipe.Domain.Migrations
{
    /// <inheritdoc />
    public partial class Update02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeManager",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrentRecipeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeManager", x => x.Id);
                    table.CheckConstraint("CK_RecipeManager_SingleRow", "[Id] = 1");
                    table.ForeignKey(
                        name: "FK_RecipeManager_Recipe_CurrentRecipeId",
                        column: x => x.CurrentRecipeId,
                        principalTable: "Recipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RecipeManager",
                columns: new[] { "Id", "CurrentRecipeId" },
                values: new object[] { 1, -1 });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeManager_CurrentRecipeId",
                table: "RecipeManager",
                column: "CurrentRecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeManager");
        }
    }
}
