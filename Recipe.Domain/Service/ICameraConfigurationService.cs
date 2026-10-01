using EFCore.IRepository;

namespace Recipe.Domain
{
  
    /// <summary>
    /// 相机配置服务接口：提供相机配置实体的通用增删改查能力。
    /// </summary>
    public interface ICameraConfigurationService : IEntityServiceBase<CameraConfiguration, Guid>
    {

    }
}
