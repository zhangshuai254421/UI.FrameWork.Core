using Framework.Alarm;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// AlarmDefinition 档案测试：MADB 十一栏的代码投影（报警哲学 §6.1【强制】）。
    /// </summary>
    public class AlarmDefinitionTests
    {
        /// <summary>档案示例：哲学 §6.1 的 HEAT01_TEMP_HI（加热板温度高于工艺上限）。</summary>
        internal static AlarmDefinition Sample() => new(
            Tag: "HEAT01_TEMP_HI",
            Description: "加热板温度高于工艺上限",
            Class: AlarmClass.Process,
            Consequence: "本炉产品全部报废",
            Cause: "加热管失控或热电偶漂移",
            OperatorAction: "停炉切手动，通知工艺员",
            MaxResponseTime: TimeSpan.FromMinutes(5),
            Severity: AlarmSeverity.S2,
            MtrTier: ResponseTimeTier.T1,
            Priority: AlarmPriority.High,
            Limit: 250,
            Deadband: 2,
            OnDelay: TimeSpan.FromSeconds(2),
            OffDelay: TimeSpan.Zero,
            Group: "加热区",
            RationalizedBy: "张三",
            RationalizedOn: new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero),
            PhilosophyVersion: "1.0");

        /// <summary>MADB 十一栏逐栏可读写：位号/描述/分类/后果/原因/动作/MTR/严重度+坐标/优先级/组态参数/评审留痕。</summary>
        [Fact]
        public void 档案_字段齐十一栏()
        {
            var def = Sample();

            Assert.Equal("HEAT01_TEMP_HI", def.Tag);                          // ① 位号
            Assert.Equal("加热板温度高于工艺上限", def.Description);          // ② 描述（含方向）
            Assert.Equal(AlarmClass.Process, def.Class);                      // ③ 分类
            Assert.Equal("本炉产品全部报废", def.Consequence);                // ④ 不处理后果
            Assert.Equal("加热管失控或热电偶漂移", def.Cause);                // ⑤ 最可能原因
            Assert.Equal("停炉切手动，通知工艺员", def.OperatorAction);       // ⑥ 操作动作（必填）
            Assert.Equal(TimeSpan.FromMinutes(5), def.MaxResponseTime);       // ⑦ MTR
            Assert.Equal(AlarmSeverity.S2, def.Severity);                     // ⑧ 严重度档
            Assert.Equal(ResponseTimeTier.T1, def.MtrTier);                   // ⑨ 矩阵坐标（S2 × T1）
            Assert.Equal(AlarmPriority.High, def.Priority);                   // ⑩ 优先级（查矩阵输出）
            Assert.Equal(250, def.Limit);                                     // ⑪ 组态参数：限值
            Assert.Equal(2, def.Deadband);                                    // ⑪ 组态参数：死区
            Assert.Equal(TimeSpan.FromSeconds(2), def.OnDelay);               // ⑪ 组态参数：触发延时
            Assert.Equal(TimeSpan.Zero, def.OffDelay);                        // ⑪ 组态参数：恢复延时
            Assert.Equal("加热区", def.Group);                                // ⑪ 组态参数：分组
            Assert.Equal("张三", def.RationalizedBy);                         // ⑫ 评审留痕：评审人
            Assert.Equal(new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero), def.RationalizedOn); // ⑫ 评审日期
            Assert.Equal("1.0", def.PhilosophyVersion);                       // ⑫ 评审留痕：哲学版本
        }

        /// <summary>负死区会把恢复点抬到限值上，报警中途就清、清完又升，永久颤抖，
        /// 这正是死区要治的毛病（哲学 §6.2），档案校验不该放它进来。</summary>
        [Fact]
        public void 校验_负死区_当庭退回()
        {
            var def = (Sample() with { Deadband = -5 });

            var ex = Assert.Throws<ArgumentException>(() => def.EnsureValid());
            Assert.Contains("死区", ex.Message);
        }

        /// <summary>NaN/Infinity 限值让滤波器永不报警（NaN 与谁比较都是假），
        /// 写进 SQLite 还会变成 NULL 读不回来，所以校验要求限值必须是有限数。</summary>
        [Fact]
        public void 校验_限值非有限数_当庭退回()
        {
            Assert.Throws<ArgumentException>(() => (Sample() with { Limit = double.NaN }).EnsureValid());
            Assert.Throws<ArgumentException>(() => (Sample() with { Deadband = double.PositiveInfinity }).EnsureValid());
        }

        /// <summary>负延时等于没有延时，触发延时和恢复延时的整定都失效（毛刺照报），校验直接拒绝。</summary>
        [Fact]
        public void 校验_负延时_当庭退回()
        {
            Assert.Throws<ArgumentException>(() => (Sample() with { OnDelay = TimeSpan.FromSeconds(-1) }).EnsureValid());
            Assert.Throws<ArgumentException>(() => (Sample() with { OffDelay = TimeSpan.FromSeconds(-1) }).EnsureValid());
        }

        /// <summary>合法组态照常通过：样例档案（死区 2、延时 2s/0、限值 250）过校验不抛。</summary>
        [Fact]
        public void 校验_合法组态_放行()
        {
            Sample().EnsureValid(); // 不抛即过
        }
    }
}
