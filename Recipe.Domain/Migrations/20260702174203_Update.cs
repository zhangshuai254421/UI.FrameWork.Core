using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipe.Domain.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
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
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: true),
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
                name: "RecipeParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Data unique identifier.")
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_CameraConfiguration_RecipeId",
                table: "CameraConfiguration",
                column: "RecipeId");

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
                name: "RecipeParameter");

            migrationBuilder.DropTable(
                name: "Recipe");
        }
    }
}
