using Framework.Alarm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 档案存储的 EF 实现（家族持久化体系，ADR-0008；#21 接棒退役的裸 SQLite 实现）：
    /// <see cref="IAlarmDefinitionStore"/> 契约的落库版——增/改前过法定校验（当庭退回），
    /// 位号主键撞车 / 改无此档由数据库约束报出、翻译成契约约定的 <see cref="ArgumentException"/>。
    /// 构造即迁移（Migrate，幂等）：与事件库同库（Alarm\AlarmDomain.db）同迁移史，
    /// 不用 EnsureCreated（对已存表静默跳过，且建出的库套不上迁移）。
    /// 线程模型：引擎启动 LoadAll、UI 线程查详情随时并发，全程加锁。
    /// 连接模型（评审留痕）：与事件库各自独享 <see cref="DataContext"/>（自带连接），
    /// 宿主接线后两存储各开连接打同一个库文件，跨连接并发写靠 SQLite 文件锁挡——
    /// busy_timeout/WAL 的拍板归 #22 宿主建库。
    /// 跟踪纪律：落库即清（ChangeTracker.Clear），写路径的跟踪只活在本口操作内；
    /// 读回一律 AsNoTracking，交还游离副本——档案「运行期只读」由实现保证，不靠调用方自律。
    /// </summary>
    public sealed class AlarmDefinitionStore : IAlarmDefinitionStore
    {
        /// <summary>增删改查四口的锁：同一 DbContext 不接受并发操作。</summary>
        private readonly object _gate = new();

        private readonly DataContext _context;

        /// <summary>用上下文选项构造存储；建库（迁移，幂等）在此时完成。</summary>
        public AlarmDefinitionStore(DbContextOptions<DataContext> options)
        {
            _context = new DataContext(options);
            _context.Database.Migrate();
        }

        /// <summary>增：先过法定校验（当庭退回）；位号已存在则抛 <see cref="ArgumentException"/>。</summary>
        public void Add(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 当庭退回：操作动作必填、触发延时 < MTR（哲学 §6.1/§6.2）

            lock (_gate)
            {
                // 落库即清的纪律下，来到这里的实例一律游离：撞号必然走到库层唯一索引，
                // 由主键违约报出、在 catch 里翻译成契约约定；其余约束违约原样外抛，不冒充「位号已存在」。
                // Clear 放 finally：撞号失败的残留跟踪也不能带进下一口操作。
                _context.AlarmDefinitions.Add(definition);
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException ex) when (IsTagCollision(ex))
                {
                    throw new ArgumentException($"位号已存在：{definition.Tag}", nameof(definition), ex);
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }
            }
        }

        /// <summary>查：按位号取档案，无则 null（「无档案不报警」的数据侧前提）。AsNoTracking 交游离副本。</summary>
        public AlarmDefinition? Find(string tag)
        {
            lock (_gate)
            {
                return _context.AlarmDefinitions.AsNoTracking().FirstOrDefault(d => d.Tag == tag);
            }
        }

        /// <summary>改：先过法定校验，位号不存在则拒；按位号整行覆盖。</summary>
        public void Update(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 改档同样过法定校验

            lock (_gate)
            {
                var existing = _context.AlarmDefinitions.Find(definition.Tag)
                    ?? throw new ArgumentException($"位号不存在：{definition.Tag}", nameof(definition));

                _context.Entry(existing).CurrentValues.SetValues(definition);
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
            }
        }

        /// <summary>删：删到返回真、没删到返回假。</summary>
        public bool Delete(string tag)
        {
            lock (_gate)
            {
                var existing = _context.AlarmDefinitions.Find(tag);
                if (existing is null)
                {
                    return false;
                }

                _context.AlarmDefinitions.Remove(existing);
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return true;
            }
        }

        /// <summary>全量加载：引擎启动时把档案全部读进内存（档案驱动，运行期只读）。AsNoTracking 交游离副本。</summary>
        public IReadOnlyList<AlarmDefinition> LoadAll()
        {
            lock (_gate)
            {
                return _context.AlarmDefinitions.AsNoTracking().ToList();
            }
        }

        /// <summary>位号主键撞车判别：SQLite UNIQUE/PRIMARY KEY 违约（与退役实现的约束码同源）。</summary>
        private static bool IsTagCollision(DbUpdateException ex)
            => ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqlite
                && sqlite.SqliteExtendedErrorCode is 2067 or 1555; // SQLITE_CONSTRAINT_UNIQUE / _PRIMARYKEY
    }
}
