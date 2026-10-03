using Framework.Core.CustomAttribute;

// 实体留在本程序集（Framework.Alarm.Domain），DbContext 住在 Framework.Alarm.Infrastructure（#21 落成）。
// 分层纪律（ADR-0008）禁止 Domain 引用 Infrastructure（会成环），所以用字符串指名而不是 typeof，
// 运行时由 AssemblyAttributeDbContextResolver 按名解析。
[assembly: DefaultDbContext("Framework.Alarm.Infrastructure.DataContext, Framework.Alarm.Infrastructure")]
