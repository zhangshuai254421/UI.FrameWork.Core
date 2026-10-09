namespace Framework.Alarm
{
    /// <summary>
    /// 内存档案库（摘要 §8.4）：字典实现，行为与 SQLite 实现同一契约（增、改都过校验，位号唯一）。
    /// 宿主演示装置、撒种脚本和测试共用这一份，替身不另养副本。
    /// </summary>
    public sealed class InMemoryAlarmDefinitionStore : IAlarmDefinitionStore
    {
        /// <summary>档案登记表：位号 → 档案。</summary>
        private readonly Dictionary<string, AlarmDefinition> _definitions = new();

        /// <summary>增：先过校验（不合法立即抛异常），位号已存在则拒。</summary>
        public void Add(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 校验：操作动作必填、触发延时 < MTR（哲学 §6.1/§6.2）

            if (!_definitions.TryAdd(definition.Tag, definition))
            {
                throw new ArgumentException($"位号已存在：{definition.Tag}", nameof(definition));
            }
        }

        /// <summary>查：按位号取档案，没有则 null（「无档案不报警」的数据侧前提）。</summary>
        public AlarmDefinition? Find(string tag) => _definitions.TryGetValue(tag, out var definition) ? definition : null;

        /// <summary>改：先过校验，位号不存在则拒。</summary>
        public void Update(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 改档案同样过校验

            if (!_definitions.ContainsKey(definition.Tag))
            {
                throw new ArgumentException($"位号不存在：{definition.Tag}", nameof(definition));
            }

            _definitions[definition.Tag] = definition;
        }

        /// <summary>删：删除成功返回真。</summary>
        public bool Delete(string tag) => _definitions.Remove(tag);

        /// <summary>全量加载：引擎启动时读全部档案。</summary>
        public IReadOnlyList<AlarmDefinition> LoadAll() => _definitions.Values.ToList();
    }
}
