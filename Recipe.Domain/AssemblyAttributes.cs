using Framework.Core.CustomAttribute;

// 实体留在本程序集（Recipe.Domain），DbContext 住在 Recipe.Infrastructure。
// 分层纪律（ADR-0008）禁止 Domain 引用 Infrastructure（会成环），所以用字符串指名而不是 typeof，
// 运行时由 AssemblyAttributeDbContextResolver 按名解析。
[assembly: DefaultDbContext("Recipe.Infrastructure.DataContext, Recipe.Infrastructure")]
