using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 内存档案存储（测试替身，摘要 §8.4「测试用内存档案」）：字典实现，行为与 SQLite 实现同一契约。
    /// </summary>
    public sealed class InMemoryAlarmDefinitionStore : IAlarmDefinitionStore
    {
        private readonly Dictionary<string, AlarmDefinition> _definitions = new();

        /// <inheritdoc/>
        public void Add(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 当庭退回：操作动作必填、触发延时 < MTR（哲学 §6.1/§6.2）

            if (!_definitions.TryAdd(definition.Tag, definition))
            {
                throw new ArgumentException($"位号已存在：{definition.Tag}", nameof(definition));
            }
        }

        /// <inheritdoc/>
        public AlarmDefinition? Find(string tag) => _definitions.TryGetValue(tag, out var definition) ? definition : null;

        /// <inheritdoc/>
        public void Update(AlarmDefinition definition)
        {
            definition.EnsureValid(); // 改档同样过法定校验

            if (!_definitions.ContainsKey(definition.Tag))
            {
                throw new ArgumentException($"位号不存在：{definition.Tag}", nameof(definition));
            }

            _definitions[definition.Tag] = definition;
        }

        /// <inheritdoc/>
        public bool Delete(string tag) => _definitions.Remove(tag);

        /// <inheritdoc/>
        public IReadOnlyList<AlarmDefinition> LoadAll() => _definitions.Values.ToList();
    }
}
