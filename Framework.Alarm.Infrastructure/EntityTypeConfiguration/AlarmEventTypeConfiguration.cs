using Framework.Alarm.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Framework.Alarm.Infrastructure.EntityTypeConfiguration
{
    /// <summary>
    /// 事件族实体的数据库映射：表 AlarmEvent，TPH 一表收七种事件（判别列 Kind = 类型名，
    /// 与退役的裸 SQLite 实现同口径——Raised/Acknowledged 直接可读，审计友好）。
    /// 自增影子主键 Id 即落库先后（append-only 本子的书写次序 = 履历次序，查询按它排）；
    /// 影子列 UtcTicks 承接时间窗过滤（时刻各带各的时区，TEXT 排序会错位，整数列才敢进 WHERE）。
    /// 子类专属列按语义并同名同列（期限并 ShelvedUntil、操作员并 Operator），异类行该列 NULL 落库即空缺；
    /// 派生型必填列（Priority/搁置期限）EF 对 TPH 自动放列可空（显式 IsRequired(false) 不被允许，默认即达意）。
    /// append-only 由实现保证：本表没有更新/删除路径，接口上就不存在（IEventStore 只有 Append/Query）。
    /// 一个类实现全族八张配置接口：派生型配置要在 ModelBuilder 上开工（EntityTypeBuilder 上没有 Entity&lt;TDerived&gt;），
    /// ApplyConfigurationsFromAssembly 对一个类的多个 IEntityTypeConfiguration&lt;T&gt; 逐个调用，正好各拿各的 builder。
    /// </summary>
    public class AlarmEventTypeConfiguration : IEntityTypeConfiguration<AlarmEvent>,
        IEntityTypeConfiguration<AlarmRaised>,
        IEntityTypeConfiguration<AlarmAcknowledged>,
        IEntityTypeConfiguration<AlarmAutoReturned>,
        IEntityTypeConfiguration<AlarmClosed>,
        IEntityTypeConfiguration<AlarmClosedByOperator>,
        IEntityTypeConfiguration<AlarmShelved>,
        IEntityTypeConfiguration<AlarmUnshelved>
    {
        /// <summary>基型：表名、影子主键/影子窗列、TPH 判别、At 落库格式。</summary>
        public void Configure(EntityTypeBuilder<AlarmEvent> builder)
        {
            builder.ToTable("AlarmEvent");

            builder.Property<int>("Id").ValueGeneratedOnAdd();  // 自增影子主键：落库先后（履历次序）
            builder.HasKey("Id");
            builder.Property<long>("UtcTicks");                 // 影子窗列：Append 时写 At.UtcTicks，查询按它筛窗

            // 判别名 = 类型名（与退役的裸 SQLite 同口径，Raised/Acknowledged 直接可读）；
            // 映射表是单一真相：判别配置在此消费，查询点名的 CLR 类型→判别名走 KindOf（#18：判别名不出存储接口）
            var discriminator = builder.HasDiscriminator<string>("Kind");
            foreach (var (type, name) in KindNames)
            {
                discriminator.HasValue(type, name);
            }

            builder.Property(e => e.At).HasConversion(StorageConverters.DateTimeOffsetO);
        }

        /// <summary>CLR 事件类型 → 判别名，全家族唯一一份：判别配置与查询点名都吃它。</summary>
        internal static readonly IReadOnlyDictionary<Type, string> KindNames = new Dictionary<Type, string>
        {
            [typeof(AlarmRaised)] = nameof(AlarmRaised),
            [typeof(AlarmAcknowledged)] = nameof(AlarmAcknowledged),
            [typeof(AlarmAutoReturned)] = nameof(AlarmAutoReturned),
            [typeof(AlarmClosed)] = nameof(AlarmClosed),
            [typeof(AlarmClosedByOperator)] = nameof(AlarmClosedByOperator),
            [typeof(AlarmShelved)] = nameof(AlarmShelved),
            [typeof(AlarmUnshelved)] = nameof(AlarmUnshelved),
        };

        /// <summary>查询点名用的映射入口：点名簿外的类型退回（与追加未知事件同口径）。</summary>
        internal static string KindOf(Type type) =>
            KindNames.TryGetValue(type, out var name) ? name : throw new NotSupportedException($"未知事件类型：{type.Name}");

        /// <summary>升级事件：优先级列 TEXT 名（异类行 NULL，EF 对 TPH 派生列自动放可空）；
        /// 搁置标列 O 格式可空，与搁置事件的期限并同列（期限就是期限，审计一处找齐）。</summary>
        public void Configure(EntityTypeBuilder<AlarmRaised> builder)
        {
            builder.Property(e => e.Priority).HasConversion<string>();
            builder.Property(e => e.ShelvedUntil).HasConversion(StorageConverters.NullableDateTimeOffsetO)
                .HasColumnName("ShelvedUntil");
        }

        /// <summary>确认事件：操作员列可空（异类行 NULL），三处操作员并同列。</summary>
        public void Configure(EntityTypeBuilder<AlarmAcknowledged> builder)
        {
            builder.Property(e => e.Operator).IsRequired(false).HasColumnName("Operator");
        }

        /// <summary>无人看见的恢复：无专属列。</summary>
        public void Configure(EntityTypeBuilder<AlarmAutoReturned> builder)
        {
        }

        /// <summary>真闭环：无专属列。</summary>
        public void Configure(EntityTypeBuilder<AlarmClosed> builder)
        {
        }

        /// <summary>操作员确认闭环（#26）：零新增列——操作员与三处并同列（可空，异类行 NULL），
        /// 判别名登记即入族；TPH 下列已存在，加派生型不动 schema。</summary>
        public void Configure(EntityTypeBuilder<AlarmClosedByOperator> builder)
        {
            builder.Property(e => e.Operator).IsRequired(false).HasColumnName("Operator");
        }

        /// <summary>搁置事件：期限（必填，非可空类型）与操作员并同列；操作员可空（异类行 NULL）。</summary>
        public void Configure(EntityTypeBuilder<AlarmShelved> builder)
        {
            builder.Property(e => e.ShelvedUntil).HasConversion(StorageConverters.DateTimeOffsetO)
                .HasColumnName("ShelvedUntil");
            builder.Property(e => e.Operator).IsRequired(false).HasColumnName("Operator");
        }

        /// <summary>解除搁置：操作员列可空（异类行 NULL），三处操作员并同列。</summary>
        public void Configure(EntityTypeBuilder<AlarmUnshelved> builder)
        {
            builder.Property(e => e.Operator).IsRequired(false).HasColumnName("Operator");
        }
    }
}
