namespace FrameWork.Device
{
    /// <summary>
    /// 设备连接参数解析辅助类
    /// </summary>
    public static class DeviceConnectionHelper
    {
        /// <summary>
        /// 从 JSON 连接参数字符串反序列化为指定类型
        /// </summary>
        public static T? ParseConnectionParams<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<T>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        /// <summary>
        /// 尝试从连接参数中获取指定 key 的值
        /// </summary>
        public static string? GetParamValue(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(key, out var value))
                    return value.GetString();
            }
            catch
            {
                // 忽略解析错误
            }

            return null;
        }
    }
}
