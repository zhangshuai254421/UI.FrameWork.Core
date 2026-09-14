using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Framework.Device.Domain.Migrations
{
    /// <inheritdoc />
    public partial class update01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceConfiguration",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, comment: "设备逻辑标识（主键）"),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Vendor = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ConnectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    PortName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfiguration", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DeviceConfiguration",
                columns: new[] { "Id", "ConnectionType", "IpAddress", "Kind", "Port", "PortName", "Vendor" },
                values: new object[,]
                {
                    { "CameraA", 1, "192.168.1.64", 1, 8000, null, "HikVision" },
                    { "MotionCard1", 2, null, 2, null, "COM1", "Leisai" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceConfiguration");
        }
    }
}
