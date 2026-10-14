using Framework.Alarm;
using Framework.Alarm.Domain;

namespace Framework.Alarm.Tests
{
    /// <summary>
    /// 排序权重联测：Rank 是「高优先级最重」的唯一出处（引擎库定，UI 只调用），
    /// 列表排序和顶栏选文本都走它——规则修订改这一处，两边不会不一致。
    /// </summary>
    public class AlarmFiltersTests
    {
        [Fact]
        public void 排序权重_高中低_依次靠前()
        {
            Assert.True(AlarmFilters.Rank(AlarmPriority.High) < AlarmFilters.Rank(AlarmPriority.Medium));
            Assert.True(AlarmFilters.Rank(AlarmPriority.Medium) < AlarmFilters.Rank(AlarmPriority.Low));
        }
    }
}
