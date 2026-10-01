using Framework.Core.CustomAttribute;

// 实体在本程序集（Framework.Device.Domain），DbContext 住在 Framework.Device.Infrastructure，
// 字符串指名避免 Domain 反向引用 Infrastructure（ADR-0008）。
[assembly: DefaultDbContext("Framework.Device.Infrastructure.DeviceDataContext, Framework.Device.Infrastructure")]
