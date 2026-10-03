using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史库的 SQLite 实现（存储 v1 直接 SQLite，ADR-0009 规则 5、摘要 §8.11）。
    /// 表在构造时幂等建立（CREATE TABLE IF NOT EXISTS），连接由宿主开好注入、生命周期归宿主。
    /// 落库映射：DateTimeOffset → ISO-8601 TEXT（保时区，审计可读）+ UtcTicks INTEGER（窗口过滤）；
    /// 事件类型 → TEXT 名字（Raised/Acknowledged 直接可读）；优先级/操作员按事件类型可空。
    /// append-only：本类只有 INSERT 与 SELECT，没有任何 UPDATE/DELETE——改写路径在代码里也不存在。
    /// 保留期 90 天（哲学 §8.1/§12 #3）是运维策略：过期清理不进 <see cref="IEventStore"/>，
    /// 届时以独立通道（定时任务直接对库执行）实现，本类不提供删除入口。
    /// 线程模型：接线后 Append 跑在引擎产事件的线程（采集层）、Query 跑在 UI 线程——
    /// 与引擎同一口径，全程加锁（与 #12 引擎总装的线程安全修正同款）。
    /// </summary>
    public sealed class SqliteEventStore : IEventStore
    {
        /// <summary>追加与查询两口的锁：同一连接不接受并发命令。</summary>
        private readonly object _gate = new();

        private readonly SqliteConnection _connection;

        /// <summary>用已打开的 SQLite 连接构造存储；表不存在则建。</summary>
        public SqliteEventStore(SqliteConnection connection)
        {
            _connection = connection;
            EnsureTable();
        }

        /// <inheritdoc/>
        public void Append(AlarmEvent e)
        {
            lock (_gate)
            {
                using var command = _connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO AlarmEvent (Kind, Tag, At, UtcTicks, Priority, Operator) " +
                    "VALUES (@Kind, @Tag, @At, @UtcTicks, @Priority, @Operator)";
                command.Parameters.AddWithValue("@Kind", KindOf(e));
                command.Parameters.AddWithValue("@Tag", e.Tag);
                command.Parameters.AddWithValue("@At", e.At.ToString("O"));
                command.Parameters.AddWithValue("@UtcTicks", e.At.UtcTicks);
                command.Parameters.AddWithValue("@Priority", e is AlarmRaised raised ? raised.Priority.ToString() : DBNull.Value);
                command.Parameters.AddWithValue("@Operator", e is AlarmAcknowledged acknowledged ? acknowledged.Operator : DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null)
        {
            lock (_gate)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT Kind, Tag, At, Priority, Operator FROM AlarmEvent " +
                    "WHERE UtcTicks >= @From AND UtcTicks <= @To";
                if (tag != null)
                {
                    command.CommandText += " AND Tag = @Tag";
                    command.Parameters.AddWithValue("@Tag", tag);
                }

                command.CommandText += " ORDER BY Seq";         // 落库先后即履历次序（append-only 本子），不靠查询计划碰运气
                command.Parameters.AddWithValue("@From", from.UtcTicks);
                command.Parameters.AddWithValue("@To", to.UtcTicks);

                using var reader = command.ExecuteReader();
                var events = new List<AlarmEvent>();
                while (reader.Read())
                {
                    events.Add(Map(reader));
                }

                return events;
            }
        }

        /// <summary>事件类型 → 落库判别名（名字直接可读，审计友好）；未知类型当庭退回——
        /// 审计本子上宁可炸得响，也不能悄悄吞掉一拍。</summary>
        private static string KindOf(AlarmEvent e) => e switch
        {
            AlarmRaised => nameof(AlarmRaised),
            AlarmAcknowledged => nameof(AlarmAcknowledged),
            AlarmAutoReturned => nameof(AlarmAutoReturned),
            AlarmClosed => nameof(AlarmClosed),
            _ => throw new NotSupportedException($"未知事件类型：{e.GetType().Name}")
        };

        /// <summary>一行读回事件；判别名路由到具体 record，时间按写入格式 invariant 往返还原
        /// （文化敏感 Parse 在非公历默认历法的区域会 FormatException）。</summary>
        private static AlarmEvent Map(SqliteDataReader reader)
        {
            var tag = reader.GetString(reader.GetOrdinal("Tag"));
            var at = DateTimeOffset.Parse(
                reader.GetString(reader.GetOrdinal("At")),
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            return reader.GetString(reader.GetOrdinal("Kind")) switch
            {
                nameof(AlarmRaised) => new AlarmRaised(
                    tag, at, Enum.Parse<AlarmPriority>(reader.GetString(reader.GetOrdinal("Priority")))),
                nameof(AlarmAcknowledged) => new AlarmAcknowledged(
                    tag, at, reader.GetString(reader.GetOrdinal("Operator"))),
                nameof(AlarmAutoReturned) => new AlarmAutoReturned(tag, at),
                nameof(AlarmClosed) => new AlarmClosed(tag, at),
                var kind => throw new InvalidOperationException($"库里有未知事件类型：{kind}")
            };
        }

        /// <summary>建表（幂等）：自增 Seq 记落库先后（履历次序）；索引盖住时间窗与位号两条查询路。
        /// 前瞻：搁置标（ShelvedUntil，摘要 §8.10）落地时本表须同步加列——历史库全量含带标事件。</summary>
        private void EnsureTable()
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS AlarmEvent (
                    Seq INTEGER PRIMARY KEY AUTOINCREMENT,
                    Kind TEXT NOT NULL,
                    Tag TEXT NOT NULL,
                    At TEXT NOT NULL,
                    UtcTicks INTEGER NOT NULL,
                    Priority TEXT,
                    Operator TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_AlarmEvent_UtcTicks ON AlarmEvent(UtcTicks);
                CREATE INDEX IF NOT EXISTS IX_AlarmEvent_Tag_UtcTicks ON AlarmEvent(Tag, UtcTicks)
                """;
            command.ExecuteNonQuery();
        }
    }
}
