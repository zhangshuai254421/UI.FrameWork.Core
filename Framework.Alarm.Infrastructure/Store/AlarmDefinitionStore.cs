using Framework.Alarm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 档案存储的 EF 实现（家族持久化体系，ADR-0008；#21 接棒退役的裸 SQLite 实现）：
    /// <see cref="IAlarmDefinitionStore"/> 契约的数据库版。增、改前先过校验（不合法立即抛异常）；
    /// 位号主键冲突、修改不存在的档案由数据库约束报出，翻译成契约约定的 <see cref="ArgumentException"/>。
    /// 建库归宿主（#22，AlarmDatabase.Initialize 启动时统一负责）：构造不再迁移，
    /// 存储只认「库已就位」这个前提；schema 演化走迁移（90 天保留期要跨版本存活，#21 拍板），
    /// 不用 EnsureCreated（它对已存在的表静默跳过，而且建出的库套不上迁移）。
    /// 线程模型：引擎启动 LoadAll、UI 线程查详情随时并发，全程加锁。
    /// 连接模型（评审留痕）：与事件库各自独享 <see cref="DataContext"/>（自带连接），
    /// 宿主接线后两个存储各开连接访问同一个库文件，跨连接并发写靠 SQLite 文件锁挡——
    /// busy_timeout/WAL 的决定归 #22 宿主建库。
    /// 跟踪纪律：写完即清（ChangeTracker.Clear），写路径的跟踪只活在本次操作内；
    /// 读回一律 AsNoTracking，交还游离副本——档案「运行期只读」由实现保证，不靠调用方自觉。
    /// </summary>
    public sealed class AlarmDefinitionStore : IAlarmDefinitionStore
    {
        /// <summary>增删改查四个入口共用的锁：同一 DbContext 不接受并发操作。</summary>
        private readonly object _gate = new();

        private readonly DataContext _context;

        /// <summary>用上下文选项构造存储；建库不在此处——宿主启动时 AlarmDatabase.Initialize 统一负责（#22）。</summary>
        public AlarmDefinitionStore(DbContextOptions<DataContext> options)
        {
            _context = new DataContext(options);
        }

        /// <summary>增：先过校验（不合法立即抛异常）；位号已存在则抛 <see cref="ArgumentException"/>。</summary>
        public void Add(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 校验：操作动作必填、触发延时 < MTR（哲学 §6.1/§6.2）

            lock (_gate)
            {
                // 写完即清的纪律下，来到这里的实例一律是游离的：主键冲突必然走到数据库层的唯一索引，
                // 由数据库报出、在 catch 里翻译成契约约定；其余约束违约原样外抛，不冒充「位号已存在」。
                // Clear 放 finally：冲突失败的残留跟踪也不能带进下一次操作。
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

        /// <summary>查：按位号取档案，没有则 null（「无档案不报警」的数据侧前提）。AsNoTracking 交游离副本。</summary>
        public AlarmDefinition? Find(string tag)
        {
            lock (_gate)
            {
                return _context.AlarmDefinitions.AsNoTracking().FirstOrDefault(d => d.Tag == tag);
            }
        }

        /// <summary>改：先过校验，位号不存在则拒；按位号整行覆盖。</summary>
        public void Update(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 改档案同样过校验

            lock (_gate)
            {
                var existing = _context.AlarmDefinitions.Find(definition.Tag)
                    ?? throw new ArgumentException($"位号不存在：{definition.Tag}", nameof(definition));

                // 写完即清放 finally（#27）：写失败（如 busy 抛 DbUpdateException）的残留跟踪
                // 不能留在长命上下文里，否则后续 Add 同位号会在跟踪器上撞键、抛契约外的
                // InvalidOperationException，主键冲突的翻译就永远轮不到了。
                try
                {
                    _context.Entry(existing).CurrentValues.SetValues(definition);
                    _context.SaveChanges();
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }
            }
        }

        /// <summary>删：删到了返回真、没删到返回假。</summary>
        public bool Delete(string tag)
        {
            lock (_gate)
            {
                var existing = _context.AlarmDefinitions.Find(tag);
                if (existing is null)
                {
                    return false;
                }

                try
                {
                    _context.AlarmDefinitions.Remove(existing);
                    _context.SaveChanges();
                }
                finally
                {
                    _context.ChangeTracker.Clear(); // 写完即清放 finally：与 Add/Update 同样的兜底（#27）
                }

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

        /// <summary>位号主键冲突判别：SQLite UNIQUE/PRIMARY KEY 违约（与退役实现的约束码一致）。</summary>
        private static bool IsTagCollision(DbUpdateException ex)
            => ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqlite
                && sqlite.SqliteExtendedErrorCode is 2067 or 1555; // SQLITE_CONSTRAINT_UNIQUE / _PRIMARYKEY
    }
}
