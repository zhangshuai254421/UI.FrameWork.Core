using Framework.Alarm.Domain;
using Framework.Alarm.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Alarm.Infrastructure.EntityTypeConfiguration
{
    /// <summary>
    /// 档案实体的数据库映射：表 AlarmDefinition，位号主键（全系统唯一的业务身份，
    /// 增/改撞号由主键违约报出）。落库映射与退役的裸 SQLite 实现同口径（审计友好）：
    /// 枚举 → TEXT 名字（S2/T1/High 直接可读）；时刻 → ISO-8601 TEXT（保时区，往返无损）。
    /// </summary>
    public class AlarmDefinitionTypeConfiguration : IEntityTypeConfiguration<AlarmDefinition>
    {
        /// <summary>配置表名、位号主键与全字段映射；MADB 无种子数据——档案来自合理化评审，代码不代笔。</summary>
        public void Configure(EntityTypeBuilder<AlarmDefinition> builder)
        {
            builder.ToTable("AlarmDefinition");
            builder.HasKey(d => d.Tag);                     // 位号即主键：唯一性约束就是「位号全系统唯一」的库侧兑现
            builder.Property(d => d.Tag).ValueGeneratedNever();

            builder.Property(d => d.Class).HasConversion<string>();
            builder.Property(d => d.Severity).HasConversion<string>();
            builder.Property(d => d.MtrTier).HasConversion<string>();
            builder.Property(d => d.Priority).HasConversion<string>();

            builder.Property(d => d.RationalizedOn).HasConversion(StorageConverters.DateTimeOffsetO);
        }
    }
}
