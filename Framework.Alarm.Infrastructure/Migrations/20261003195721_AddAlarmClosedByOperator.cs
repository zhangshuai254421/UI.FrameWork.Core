using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Framework.Alarm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmClosedByOperator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 零 schema 变化（#26）：AlarmClosedByOperator 是 TPH 派生型——操作员列（三处并同列）
            // 与判别列都已在 InitialCreate 里，判别值是行数据不是 schema；本迁移只同步模型快照。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
