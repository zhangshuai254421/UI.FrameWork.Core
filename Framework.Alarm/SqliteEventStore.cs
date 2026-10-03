using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Framework.Alarm
{
    /// <summary>
    /// 事件历史库的 SQLite 实现（存储 v1 直接 SQLite，ADR-0009 规则 5、摘要 §8.11）。
    /// 表在构造时幂等建立（CREATE TABLE IF NOT EXISTS），连接由宿主开好注入、生命周期归宿主。
    /// 落库映射：DateTimeOffset → ISO-8601 TEXT（保时区，审计可读）+ UtcTicks INTEGER（窗口过滤）；
    /// 事件类型 → TEXT 名字（Raised/Acknowledged 直接可读）；优先级/操作员/搁置标按事件类型可空
    /// （搁置标：ShelvedUntil，摘要 §8.10——历史库全量含带标事件）。
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
                    "INSERT INTO AlarmEvent (Kind, Tag, At, UtcTicks, Priority, Operator, ShelvedUntil) " +
                    "VALUES (@Kind, @Tag, @At, @UtcTicks, @Priority, @Operator, @ShelvedUntil)";
                command.Parameters.AddWithValue("@Kind", KindOf(e));
                command.Parameters.AddWithValue("@Tag", e.Tag);
                command.Parameters.AddWithValue("@At", e.At.ToString("O"));
                command.Parameters.AddWithValue("@UtcTicks", e.At.UtcTicks);
                command.Parameters.AddWithValue("@Priority", PriorityOf(e));
                command.Parameters.AddWithValue("@Operator", OperatorOf(e));
                command.Parameters.AddWithValue("@ShelvedUntil", ShelvedUntilOf(e));
                command.ExecuteNonQuery();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<AlarmEvent> Query(DateTimeOffset from, DateTimeOffset to, string? tag = null, params Type[] kinds)
        {
            lock (_gate)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT Kind, Tag, At, Priority, Operator, ShelvedUntil FROM AlarmEvent " +
                    "WHERE UtcTicks >= @From AND UtcTicks <= @To";
                if (tag != null)
                {
                    command.CommandText += " AND Tag = @Tag";
                    command.Parameters.AddWithValue("@Tag", tag);
                }

                if (kinds is { Length: > 0 })                   // 种类筛在库里做（#18）：别把整本子物化进内存再扔
                {
                    var names = kinds.Select(KindOfTypeOf).ToArray(); // 先验后拼：点名簿外的类型当庭退回，拼不出半句 SQL
                    var placeholders = names.Select((_, i) => $"@Kind{i}");
                    command.CommandText += $" AND Kind IN ({string.Join(", ", placeholders)})";
                    for (var i = 0; i < names.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@Kind{i}", names[i]);
                    }
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
        private static string KindOf(AlarmEvent e) => KindOfTypeOf(e.GetType());

        /// <summary>CLR 事件类型 → 判别名（#18 查询点名走它，落库判别同源）：点名簿外的类型退回。</summary>
        private static string KindOfTypeOf(Type type) => type == typeof(AlarmRaised) ? nameof(AlarmRaised)
            : type == typeof(AlarmAcknowledged) ? nameof(AlarmAcknowledged)
            : type == typeof(AlarmAutoReturned) ? nameof(AlarmAutoReturned)
            : type == typeof(AlarmClosed) ? nameof(AlarmClosed)
            : type == typeof(AlarmShelved) ? nameof(AlarmShelved)
            : type == typeof(AlarmUnshelved) ? nameof(AlarmUnshelved)
            : throw new NotSupportedException($"未知事件类型：{type.Name}");

        /// <summary>优先级列：升级事件查矩阵的输出留档，其余事件该列为空。</summary>
        private static object PriorityOf(AlarmEvent e) => e switch
        {
            AlarmRaised raised => raised.Priority.ToString(),
            _ => DBNull.Value
        };

        /// <summary>操作员列：确认/搁置/解除三类操作员事件留名，其余事件该列为空。</summary>
        private static object OperatorOf(AlarmEvent e) => e switch
        {
            AlarmAcknowledged acknowledged => acknowledged.Operator,
            AlarmShelved shelved => shelved.Operator,
            AlarmUnshelved unshelved => unshelved.Operator,
            _ => DBNull.Value
        };

        /// <summary>搁置标列：带标的升级（搁置中触发）与搁置动作留期限，其余事件该列为空。</summary>
        private static object ShelvedUntilOf(AlarmEvent e) => e switch
        {
            AlarmRaised { ShelvedUntil: not null } raised => raised.ShelvedUntil.Value.ToString("O"),
            AlarmShelved shelved => shelved.ShelvedUntil.ToString("O"),
            _ => DBNull.Value
        };

        /// <summary>一行读回事件；判别名路由到具体 record，时间按写入格式 invariant 往返还原
        /// （文化敏感 Parse 在非公历默认历法的区域会 FormatException）；可空列先探 DBNull。</summary>
        private static AlarmEvent Map(SqliteDataReader reader)
        {
            var tag = reader.GetString(reader.GetOrdinal("Tag"));
            var at = DateTimeOffset.Parse(
                reader.GetString(reader.GetOrdinal("At")),
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var shelvedUntil = ReadNullableTime(reader, "ShelvedUntil");

            return reader.GetString(reader.GetOrdinal("Kind")) switch
            {
                nameof(AlarmRaised) => new AlarmRaised(
                    tag, at, Enum.Parse<AlarmPriority>(reader.GetString(reader.GetOrdinal("Priority"))),
                    ShelvedUntil: shelvedUntil),
                nameof(AlarmAcknowledged) => new AlarmAcknowledged(
                    tag, at, reader.GetString(reader.GetOrdinal("Operator"))),
                nameof(AlarmAutoReturned) => new AlarmAutoReturned(tag, at),
                nameof(AlarmClosed) => new AlarmClosed(tag, at),
                nameof(AlarmShelved) => new AlarmShelved(
                    tag, at, shelvedUntil!.Value, reader.GetString(reader.GetOrdinal("Operator"))),
                nameof(AlarmUnshelved) => new AlarmUnshelved(
                    tag, at, reader.GetString(reader.GetOrdinal("Operator"))),
                var kind => throw new InvalidOperationException($"库里有未知事件类型：{kind}")
            };
        }

        /// <summary>可空时间列：NULL 回 null，否则 invariant 往返还原。</summary>
        private static DateTimeOffset? ReadNullableTime(SqliteDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal)
                ? null
                : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        /// <summary>建表（幂等）：自增 Seq 记落库先后（履历次序）；索引盖住时间窗与位号两条查询路。
        /// 搁置标列（ShelvedUntil，摘要 §8.10——历史库全量含带标事件）随建表语句立列；
        /// 旧库没有此列的，用 PRAGMA 探测后 ALTER TABLE 补上（SQLite 加列只能追加 nullable，正合语义）。</summary>
        private void EnsureTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS AlarmEvent (
                        Seq INTEGER PRIMARY KEY AUTOINCREMENT,
                        Kind TEXT NOT NULL,
                        Tag TEXT NOT NULL,
                        At TEXT NOT NULL,
                        UtcTicks INTEGER NOT NULL,
                        Priority TEXT,
                        Operator TEXT,
                        ShelvedUntil TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_AlarmEvent_UtcTicks ON AlarmEvent(UtcTicks);
                    CREATE INDEX IF NOT EXISTS IX_AlarmEvent_Tag_UtcTicks ON AlarmEvent(Tag, UtcTicks)
                    """;
                command.ExecuteNonQuery();
            }

            if (!HasColumn("AlarmEvent", "ShelvedUntil"))
            {
                using var migration = _connection.CreateCommand();
                migration.CommandText = "ALTER TABLE AlarmEvent ADD COLUMN ShelvedUntil TEXT";
                migration.ExecuteNonQuery();
            }
        }

        /// <summary>探测表里是否已有某列（旧库迁移判断）。</summary>
        private bool HasColumn(string table, string column)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({table})";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1) == column) // table_info 列序：1 = name
                {
                    return true;
                }
            }

            return false;
        }
    }
}
