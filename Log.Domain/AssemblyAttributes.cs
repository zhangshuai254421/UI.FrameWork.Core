using Framework.Core.CustomAttribute;

// 同 Recipe.Domain：实体留在本程序集，DbContext 住在 Log.Infrastructure，
// 字符串指名避免 Domain 反向引用 Infrastructure（ADR-0008）。
[assembly: DefaultDbContext("Log.Infrastructure.DataContext, Log.Infrastructure")]
