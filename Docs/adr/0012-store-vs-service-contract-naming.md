# 契约命名边界：持久化叫 Store，编排才叫 Service

Status: accepted（2026-10-10 定；词汇表词条同步进 CONTEXT.md）

## 决定

数据契约的后缀按行为定，不按习惯定：

1. **Store = 持久化读写**：增删改查加全量加载，进门前可拒不合法数据，此外不含业务。新家族的持久化契约一律叫 Store，样板是报警家族的 `IAlarmDefinitionStore` 与 `IEventStore`。
2. **Service = 业务编排**：跨实体协调、流程规则。命名门槛是真有编排，不是替数据库跑腿。
3. **旧家族不追溯**：Recipe 家族的 `IRecipeManagerService` 等 XxxService 继承 `IEntityServiceBase`，全是纯 CRUD 却叫 Service。照旧不改名；新代码不许学。

## 为什么是 Store 不是 Service

Service 是软件里被过载最严重的词，看见 Service 猜不出内容；Store 语义窄且稳，看见就猜「持久化」，基本不猜错。而本仓库的实际状态是：报警家族已经用 Store，Recipe 家族用 Service，同一个层两个名字并存——放任不管才是最坏的。往 Store 方向收敛，因为旧家族的 Service 名实不符（名字承诺编排，实际只交付 CRUD），名字应该跟行为走。

## 后果

- `Framework.Alarm.Domain` 的契约目录由 `Service/` 更名 `Store/`（2026-10-10）；命名空间本就是 `Framework.Alarm.Domain`，零代码影响。
- Recipe 家族两套后缀并存会持续一段时间；读旧代码时把 XxxService 读作 Store 即可，不要顺手改名，churn 无收益。
- 共享内核 `IEntityServiceBase`（`EFCore.IRepository`）名字里的 Service 同样不追溯，它本质是泛型仓储；报警家族没有依赖它，持久化契约自建接缝，后续新家族可循此例。
