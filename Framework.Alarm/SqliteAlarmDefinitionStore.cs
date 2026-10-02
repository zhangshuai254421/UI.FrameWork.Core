using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Framework.Alarm
{
    /// <summary>
    /// 档案存储的 SQLite 实现（存储 v1 直接 SQLite，ADR-0009 规则 5、摘要 §8.4）。
    /// 表在构造时幂等建立（CREATE TABLE IF NOT EXISTS），连接由宿主开好注入、生命周期归宿主。
    /// 落库映射：DateTimeOffset → ISO-8601 TEXT（保时区，往返无损）；TimeSpan → INTEGER ticks；
    /// 枚举 → TEXT 名字（S2/T1/High 直接可读，审计友好）；<c>Limit</c>/<c>Group</c> 是 SQL 关键字，标识符加引号。
    /// </summary>
    public sealed class SqliteAlarmDefinitionStore : IAlarmDefinitionStore
    {
        private const string Columns =
            "Tag, Description, Class, Consequence, Cause, OperatorAction, MaxResponseTime, Severity, " +
            "MtrTier, Priority, \"Limit\", Deadband, OnDelay, OffDelay, \"Group\", " +
            "RationalizedBy, RationalizedOn, PhilosophyVersion";

        private readonly SqliteConnection _connection;

        /// <summary>用已打开的 SQLite 连接构造存储；表不存在则建。</summary>
        public SqliteAlarmDefinitionStore(SqliteConnection connection)
        {
            _connection = connection;
            EnsureTable();
        }

        /// <inheritdoc/>
        public void Add(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 当庭退回：操作动作必填、触发延时 < MTR（哲学 §6.1/§6.2）

            using var command = _connection.CreateCommand();
            command.CommandText =
                $"INSERT INTO AlarmDefinition ({Columns}) " +
                "VALUES (@Tag, @Description, @Class, @Consequence, @Cause, @OperatorAction, @MaxResponseTime, " +
                "@Severity, @MtrTier, @Priority, @Limit, @Deadband, @OnDelay, @OffDelay, @Group, " +
                "@RationalizedBy, @RationalizedOn, @PhilosophyVersion)";
            Bind(command, definition);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode is 2067 or 1555)
            // SQLITE_CONSTRAINT_UNIQUE / _PRIMARYKEY：位号撞车；其余约束违约（NOT NULL 等）原样外抛，不冒充「位号已存在」
            {
                throw new ArgumentException($"位号已存在：{definition.Tag}", nameof(definition), ex);
            }
        }

        /// <inheritdoc/>
        public AlarmDefinition? Find(string tag)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"SELECT {Columns} FROM AlarmDefinition WHERE Tag = @Tag";
            command.Parameters.AddWithValue("@Tag", tag);

            using var reader = command.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        /// <inheritdoc/>
        public void Update(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 改档同样过法定校验

            using var command = _connection.CreateCommand();
            command.CommandText =
                "UPDATE AlarmDefinition SET Description = @Description, Class = @Class, Consequence = @Consequence, " +
                "Cause = @Cause, OperatorAction = @OperatorAction, MaxResponseTime = @MaxResponseTime, " +
                "Severity = @Severity, MtrTier = @MtrTier, Priority = @Priority, \"Limit\" = @Limit, " +
                "Deadband = @Deadband, OnDelay = @OnDelay, OffDelay = @OffDelay, \"Group\" = @Group, " +
                "RationalizedBy = @RationalizedBy, RationalizedOn = @RationalizedOn, PhilosophyVersion = @PhilosophyVersion " +
                "WHERE Tag = @Tag";
            Bind(command, definition);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new ArgumentException($"位号不存在：{definition.Tag}", nameof(definition));
            }
        }

        /// <inheritdoc/>
        public bool Delete(string tag)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM AlarmDefinition WHERE Tag = @Tag";
            command.Parameters.AddWithValue("@Tag", tag);
            return command.ExecuteNonQuery() > 0;
        }

        /// <inheritdoc/>
        public IReadOnlyList<AlarmDefinition> LoadAll()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"SELECT {Columns} FROM AlarmDefinition";

            using var reader = command.ExecuteReader();
            var definitions = new List<AlarmDefinition>();
            while (reader.Read())
            {
                definitions.Add(Map(reader));
            }

            return definitions;
        }

        /// <summary>建表（幂等）：位号主键即唯一性约束。</summary>
        private void EnsureTable()
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS AlarmDefinition (
                    Tag TEXT PRIMARY KEY,
                    Description TEXT NOT NULL,
                    Class TEXT NOT NULL,
                    Consequence TEXT NOT NULL,
                    Cause TEXT NOT NULL,
                    OperatorAction TEXT NOT NULL,
                    MaxResponseTime INTEGER NOT NULL,
                    Severity TEXT NOT NULL,
                    MtrTier TEXT NOT NULL,
                    Priority TEXT NOT NULL,
                    "Limit" REAL NOT NULL,
                    Deadband REAL NOT NULL,
                    OnDelay INTEGER NOT NULL,
                    OffDelay INTEGER NOT NULL,
                    "Group" TEXT NOT NULL,
                    RationalizedBy TEXT NOT NULL,
                    RationalizedOn TEXT NOT NULL,
                    PhilosophyVersion TEXT NOT NULL
                )
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>整行参数绑定：INSERT 与 UPDATE 共用。</summary>
        private static void Bind(SqliteCommand command, AlarmDefinition definition)
        {
            command.Parameters.AddWithValue("@Tag", definition.Tag);
            command.Parameters.AddWithValue("@Description", definition.Description);
            command.Parameters.AddWithValue("@Class", definition.Class.ToString());
            command.Parameters.AddWithValue("@Consequence", definition.Consequence);
            command.Parameters.AddWithValue("@Cause", definition.Cause);
            command.Parameters.AddWithValue("@OperatorAction", definition.OperatorAction);
            command.Parameters.AddWithValue("@MaxResponseTime", definition.MaxResponseTime.Ticks);
            command.Parameters.AddWithValue("@Severity", definition.Severity.ToString());
            command.Parameters.AddWithValue("@MtrTier", definition.MtrTier.ToString());
            command.Parameters.AddWithValue("@Priority", definition.Priority.ToString());
            command.Parameters.AddWithValue("@Limit", definition.Limit);
            command.Parameters.AddWithValue("@Deadband", definition.Deadband);
            command.Parameters.AddWithValue("@OnDelay", definition.OnDelay.Ticks);
            command.Parameters.AddWithValue("@OffDelay", definition.OffDelay.Ticks);
            command.Parameters.AddWithValue("@Group", definition.Group);
            command.Parameters.AddWithValue("@RationalizedBy", definition.RationalizedBy);
            command.Parameters.AddWithValue("@RationalizedOn", definition.RationalizedOn.ToString("O"));
            command.Parameters.AddWithValue("@PhilosophyVersion", definition.PhilosophyVersion);
        }

        /// <summary>一行读回档案；枚举按名字解析、时间按写入格式还原。</summary>
        private static AlarmDefinition Map(SqliteDataReader reader)
        {
            return new AlarmDefinition(
                reader.GetString(reader.GetOrdinal("Tag")),
                reader.GetString(reader.GetOrdinal("Description")),
                Enum.Parse<AlarmClass>(reader.GetString(reader.GetOrdinal("Class"))),
                reader.GetString(reader.GetOrdinal("Consequence")),
                reader.GetString(reader.GetOrdinal("Cause")),
                reader.GetString(reader.GetOrdinal("OperatorAction")),
                new TimeSpan(reader.GetInt64(reader.GetOrdinal("MaxResponseTime"))),
                Enum.Parse<AlarmSeverity>(reader.GetString(reader.GetOrdinal("Severity"))),
                Enum.Parse<ResponseTimeTier>(reader.GetString(reader.GetOrdinal("MtrTier"))),
                Enum.Parse<AlarmPriority>(reader.GetString(reader.GetOrdinal("Priority"))),
                reader.GetDouble(reader.GetOrdinal("Limit")),
                reader.GetDouble(reader.GetOrdinal("Deadband")),
                new TimeSpan(reader.GetInt64(reader.GetOrdinal("OnDelay"))),
                new TimeSpan(reader.GetInt64(reader.GetOrdinal("OffDelay"))),
                reader.GetString(reader.GetOrdinal("Group")),
                reader.GetString(reader.GetOrdinal("RationalizedBy")),
                // 写入是 invariant "O" 格式，读回必须配 invariant roundtrip 解析——文化敏感 Parse
                // 在非公历默认历法的区域（如乌姆阿拉历）会 FormatException，引擎启动即挂
                DateTimeOffset.Parse(
                    reader.GetString(reader.GetOrdinal("RationalizedOn")),
                    CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                reader.GetString(reader.GetOrdinal("PhilosophyVersion")));
        }
    }
}
