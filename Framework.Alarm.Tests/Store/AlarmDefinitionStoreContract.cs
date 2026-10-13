using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 档案存储契约：内存替身与 SQLite 实现共用同一套行为断言，两套各测一遍。
    /// 契约方法只断言、不建存储，存储由各测试套件注入。
    /// </summary>
    internal static class AlarmDefinitionStoreContract
    {
        /// <summary>增后查：存进去的原样读回来（record 值相等）。</summary>
        public static void 增_新档_查回原档(IAlarmDefinitionStore store)
        {
            var def = AlarmDefinitionTests.Sample();

            store.Add(def);

            var found = store.Find("HEAT01_TEMP_HI");
            Assert.NotNull(found);
            Assert.Equal(def, found);
        }

        /// <summary>查无此档：返回 null（引擎「无档案不报警」的数据侧前提）。</summary>
        public static void 查_无此档_返回空(IAlarmDefinitionStore store)
        {
            Assert.Null(store.Find("NOT_EXIST"));
        }

        /// <summary>改：同位号整行覆盖，读回为新值。</summary>
        public static void 改_同位号_整行覆盖(IAlarmDefinitionStore store)
        {
            store.Add(AlarmDefinitionTests.Sample());
            var revised = AlarmDefinitionTests.Sample() with { Limit = 260, PhilosophyVersion = "1.1" }; // 限值改版重评审

            store.Update(revised);

            var found = store.Find("HEAT01_TEMP_HI");
            Assert.NotNull(found);
            Assert.Equal(260, found.Limit);
            Assert.Equal("1.1", found.PhilosophyVersion);
        }

        /// <summary>删：删后查无，返回真；再删返回假。</summary>
        public static void 删_删后查无(IAlarmDefinitionStore store)
        {
            store.Add(AlarmDefinitionTests.Sample());

            Assert.True(store.Delete("HEAT01_TEMP_HI"));
            Assert.Null(store.Find("HEAT01_TEMP_HI"));
            Assert.False(store.Delete("HEAT01_TEMP_HI")); // 重复删：假，不抛
        }

        /// <summary>全量加载：按任意顺序增三条，全量取回条数与内容都齐。</summary>
        public static void 全量加载_三条档_条目齐全(IAlarmDefinitionStore store)
        {
            store.Add(AlarmDefinitionTests.Sample());
            store.Add(AlarmDefinitionTests.Sample() with { Tag = "HEAT01_TEMP_HIHI" });
            store.Add(AlarmDefinitionTests.Sample() with { Tag = "PUMP01_STOP" });

            var all = store.LoadAll();

            Assert.Equal(3, all.Count);
            Assert.Contains(all, d => d.Tag == "HEAT01_TEMP_HI");
            Assert.Contains(all, d => d.Tag == "HEAT01_TEMP_HIHI");
            Assert.Contains(all, d => d.Tag == "PUMP01_STOP");
        }

        /// <summary>位号全系统唯一：重复增同一档案被拒。</summary>
        public static void 增_重复位号_被拒(IAlarmDefinitionStore store)
        {
            store.Add(AlarmDefinitionTests.Sample());

            Assert.Throws<ArgumentException>(() => store.Add(AlarmDefinitionTests.Sample()));
        }

        /// <summary>操作动作必填（哲学 §6.1）：缺动作的档案增不进，校验不通过立即抛异常。</summary>
        public static void 增_操作动作缺_校验即抛(IAlarmDefinitionStore store)
        {
            var def = AlarmDefinitionTests.Sample() with { OperatorAction = "" };

            Assert.Throws<ArgumentException>(() => store.Add(def));
        }

        /// <summary>铁律一（哲学 §6.2）：触发延时 ≥ MTR 的档案被拒，响应窗口不能被延时吞尽。</summary>
        public static void 增_触发延时逾MTR_被拒(IAlarmDefinitionStore store)
        {
            var def = AlarmDefinitionTests.Sample() with { OnDelay = TimeSpan.FromMinutes(5) }; // 恰等于 MTR，不小于

            Assert.Throws<ArgumentException>(() => store.Add(def));
        }

        /// <summary>改档同样过全套校验：非法改档当场抛异常，原档不受影响。</summary>
        public static void 改_操作动作缺_校验即抛(IAlarmDefinitionStore store)
        {
            store.Add(AlarmDefinitionTests.Sample());
            var bad = AlarmDefinitionTests.Sample() with { OperatorAction = "  " };

            Assert.Throws<ArgumentException>(() => store.Update(bad));

            var found = store.Find("HEAT01_TEMP_HI");
            Assert.NotNull(found);
            Assert.Equal("停炉切手动，通知工艺员", found.OperatorAction); // 原档未被污染
        }
    }
}
