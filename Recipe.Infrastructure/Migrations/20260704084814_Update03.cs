using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Recipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Data unique identifier.")
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeName = table.Column<string>(type: "TEXT", nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", nullable: false),
                    MachineName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CameraConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Data unique identifier.")
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CameraName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CameraConfiguration_Recipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "RecipeParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Data unique identifier.")
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeParameter_Recipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Recipe",
                columns: new[] { "Id", "GroupName", "MachineName", "RecipeName" },
                values: new object[] { -1, "-Default-", "ZS1A", "default" });

            migrationBuilder.InsertData(
                table: "CameraConfiguration",
                columns: new[] { "Id", "CameraName", "RecipeId" },
                values: new object[,]
                {
                    { 1, "CameraA", -1 },
                    { 2, "CameraB", -1 },
                    { 3, "CameraC", -1 },
                    { 4, "CameraD", -1 }
                });

            migrationBuilder.InsertData(
                table: "RecipeManager",
                columns: new[] { "Id", "CurrentRecipeId" },
                values: new object[] { 1, -1 });

            migrationBuilder.CreateIndex(
                name: "IX_CameraConfiguration_RecipeId",
                table: "CameraConfiguration",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeManager_CurrentRecipeId",
                table: "RecipeManager",
                column: "CurrentRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeParameter_RecipeId",
                table: "RecipeParameter",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CameraConfiguration");

            migrationBuilder.DropTable(
                name: "RecipeManager");

            migrationBuilder.DropTable(
                name: "RecipeParameter");

            migrationBuilder.DropTable(
                name: "Recipe");
        }
    }
}
