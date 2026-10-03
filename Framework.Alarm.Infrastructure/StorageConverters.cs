using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Framework.Alarm.Infrastructure
{
    /// <summary>
    /// 报警家族共用转换器：时刻一律 ISO-8601 O 格式、不变文化落 TEXT（带时区偏移，往返无损，直接可读）。
    /// 可空/非可空两个形态：EF 按 CLR 属性类型取用——档案 RationalizedOn 与搁置期限是非空时刻，
    /// 升级事件的搁置标是可空时刻；三处共用，转换器只写一遍（评审：重复代码收口）。
    /// </summary>
    internal static class StorageConverters
    {
        /// <summary>非可空时刻 ↔ O 格式 TEXT。</summary>
        public static readonly ValueConverter<DateTimeOffset, string> DateTimeOffsetO = new(
            v => v.ToString("O", CultureInfo.InvariantCulture),
            v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

        /// <summary>可空时刻 ↔ O 格式 TEXT（空值不进转换器，库里落 NULL）。</summary>
        public static readonly ValueConverter<DateTimeOffset?, string> NullableDateTimeOffsetO = new(
            v => v.Value.ToString("O", CultureInfo.InvariantCulture),
            v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}
