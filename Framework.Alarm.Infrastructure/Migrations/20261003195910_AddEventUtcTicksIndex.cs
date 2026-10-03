using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Framework.Alarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventUtcTicksIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvent_UtcTicks",
                table: "AlarmEvent",
                column: "UtcTicks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlarmEvent_UtcTicks",
                table: "AlarmEvent");
        }
    }
}
