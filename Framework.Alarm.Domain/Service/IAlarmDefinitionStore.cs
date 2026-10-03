namespace Framework.Alarm.Domain
{
    /// <summary>
    /// 档案存储接缝（ADR-0009 规则 5，摘要 §8.4）：MADB 档案的增删改查与全量加载。
    /// 引擎只认本接缝，不直连数据库——测试注内存替身，宿主注 SQLite 实现。
    /// 位号全系统唯一：重复增同一档案被拒（<see cref="ArgumentException"/>）。
    /// </summary>
    public interface IAlarmDefinitionStore
    {
        /// <summary>增：新增一条档案；位号已存在则抛 <see cref="ArgumentException"/>。</summary>
        void Add(AlarmDefinition definition);

        /// <summary>查：按位号取档案；无则返回 null（「无档案不报警」的数据侧前提）。</summary>
        AlarmDefinition? Find(string tag);

        /// <summary>改：按位号整行覆盖；位号不存在则抛 <see cref="ArgumentException"/>。</summary>
        void Update(AlarmDefinition definition);

        /// <summary>删：按位号删除，删到返回真、没删到返回假。</summary>
        bool Delete(string tag);

        /// <summary>全量加载：引擎启动时把档案全部读进内存（档案驱动，运行期只读）。</summary>
        IReadOnlyList<AlarmDefinition> LoadAll();
    }
}
