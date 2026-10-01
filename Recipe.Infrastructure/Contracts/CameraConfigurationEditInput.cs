namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 相机名称编辑契约（ADR 0002）：既是 <see cref="CameraConfigurationService.GetCameraNameEditListAsync"/>
    /// 的查询投影结果，也是 <see cref="CameraConfigurationService.SaveCameraNameEditAsync"/> 的保存入参。
    /// UI 侧只对它做 INPC 薄包装，全程不接触实体类型；不保存直接丢弃即为取消。
    /// </summary>
    public class CameraConfigurationEditInput
    {
        /// <summary>对应相机配置实体的 Id。</summary>
        public int Id { get; set; }

        /// <summary>编辑中的相机名称。</summary>
        public string CameraName { get; set; } = string.Empty;
    }
}
