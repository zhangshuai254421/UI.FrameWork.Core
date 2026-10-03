using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Framework.Alarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmDefinition",
                columns: table => new
                {
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Class = table.Column<string>(type: "TEXT", nullable: false),
                    Consequence = table.Column<string>(type: "TEXT", nullable: false),
                    Cause = table.Column<string>(type: "TEXT", nullable: false),
                    OperatorAction = table.Column<string>(type: "TEXT", nullable: false),
                    MaxResponseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    MtrTier = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    Limit = table.Column<double>(type: "REAL", nullable: false),
                    Deadband = table.Column<double>(type: "REAL", nullable: false),
                    OnDelay = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    OffDelay = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Group = table.Column<string>(type: "TEXT", nullable: false),
                    RationalizedBy = table.Column<string>(type: "TEXT", nullable: false),
                    RationalizedOn = table.Column<string>(type: "TEXT", nullable: false),
                    PhilosophyVersion = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmDefinition", x => x.Tag);
                });

            migrationBuilder.CreateTable(
                name: "AlarmEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    At = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    UtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Operator = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<string>(type: "TEXT", nullable: true),
                    ShelvedUntil = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEvent", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmDefinition");

            migrationBuilder.DropTable(
                name: "AlarmEvent");
        }
    }
}
