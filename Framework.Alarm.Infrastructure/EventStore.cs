using Framework.Alarm.Domain;
using Framework.Alarm.Infrastructure.EntityTypeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 事件历史库的 EF 实现（家族持久化体系，ADR-0008；#21 接棒退役的裸 SQLite 实现）：
    /// <see cref="IEventStore"/> 契约的落库版。append-only 铁律不因换 EF 而松——
    /// 本类只有 Add 与查询，没有任何 Update/Remove 调用，改写路径在代码里不存在；
    /// 保留期 90 天（哲学 §8.1/§12 #3）是运维策略，过期清理不进本接口。
    /// 构造即迁移（Migrate，幂等）：事件史要跨版本存活（90 天保留），schema 演化走迁移，
    /// 不用 EnsureCreated（对已存表静默跳过，且建出的库套不上迁移——接盘即弃库）。
    /// 线程模型：Append 跑在引擎产事件的线程、Query 跑在 UI 线程，全程加锁。
    /// 连接模型（评审留痕）：存储实例独享 <see cref="DataContext"/>（自带连接），
    /// 与档案库互不串锁；但宿主接线后两存储各开连接打同一个库文件，跨连接并发写
    /// 靠 SQLite 文件锁挡——busy_timeout/WAL 的拍板归 #22 宿主建库。
    /// 跟踪纪律：落库即清（ChangeTracker.Clear）——append-only 本子若不清，跟踪器只增不减；
    /// 读回一律 AsNoTracking，交还游离副本，「运行期只读」由实现保证，不靠调用方自律。
    /// </summary>
    public sealed class EventStore : IEventStore
    {
        /// <summary>追加与查询两口的锁：同一 DbContext 不接受并发操作。</summary>
        private readonly object _gate = new();

        private readonly DataContext _context;

        /// <summary>用上下文选项构造存储；建库（迁移，幂等）在此时完成。</summary>
        public EventStore(DbContextOptions<DataContext> options)
        {
            _context = new DataContext(options);
            _context.Database.Migrate();
        }

        /// <summary>追加：只进不改，一条事件一行，永不覆盖、永不删除（同一实例二度追加也是重开一行——
        /// 落库即清的纪律保证每次见到的都是游离实例，Add 就是新行，库重新发号）。
        /// 影子窗列 UtcTicks 在此时写定（At 各带各的时区，整数列才敢进 WHERE）。</summary>
        public void Append(AlarmEvent e)
        {
            lock (_gate)
            {
                _context.AlarmEvents.Add(e);
                _context.Entry(e).Property("UtcTicks").CurrentValue = e.At.UtcTicks;
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
            }
        }

        /// <summary>查询：按事件时刻窗口 [from, to]（闭区间，UtcTicks 整数比较）、可选位号与可选种类过滤（#18）。
        /// kinds 点 CLR 事件类型（调用方不认得库内判别名，判别名不出存储接口）；null/空 = 全类。
        /// 结果按落库先后（自增 Id）返回——append-only 本子的书写次序即履历次序，过滤只筛不排。</summary>
        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
        {
            // 先验后查：点名簿外的类型当庭退回，拼不出半句 SQL（与 Append 未知事件同口径）
            var names = kinds is { Length: > 0 }
                ? kinds.Select(AlarmEventTypeConfiguration.KindOf).ToArray()
                : null;

            lock (_gate)
            {
                IQueryable<AlarmEvent> query = _context.AlarmEvents
                    .AsNoTracking()
                    .Where(e => EF.Property<long>(e, "UtcTicks") >= from.UtcTicks
                             && EF.Property<long>(e, "UtcTicks") <= to.UtcTicks);
                if (tag != null)
                {
                    query = query.Where(e => e.Tag == tag);
                }

                if (names is not null)                          // 种类筛在库里做（#18）：别把整本子物化进内存再扔
                {
                    query = query.Where(e => names.Contains(EF.Property<string>(e, "Kind")!));
                }

                return query.OrderBy(e => EF.Property<int>(e, "Id")).ToList();
            }
        }
    }
}
