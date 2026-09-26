# EF 实体不出数据层：Service 层投影契约类型，单向手写映射

Status: accepted

核心原则一句话：全系统只有一个方向的手写映射（实体 → 契约类型）；UI 的世界里没有实体。

## 数据流

```
【读】 DbContext → Repository(AsNoTracking，现状不变) → Service 内 Select 投影 → 返回 DTO → VM 直绑
【写】 VM(BindableBase 薄包装) → 打包 SaveXxxInput → service.Save(input) → 实体赋值 + SaveChanges → 发事件
【推】 service 写操作完成 → IEventAggregator 发布(Added/Updated/Removed + DTO) → VM 按 Id 整条替换/插入/移除
```

编辑 VM 是独立对象，从生到死没碰过数据库——取消就是扔掉它，不存在回滚。

## 规则

1. **实体不出域**：实体类型只存在于 Domain/Infrastructure；UI 工程只使用 DTO / Input / 枚举。契约类型跟随各自 Infrastructure 项目存放，枚举也从实体项目挪到对应 Infrastructure（不加新项目）。
2. **映射只有两处、全部手写**：service 内的查询投影（实体→DTO），UI 内的薄包装（DTO→BindableBase VM）。不引入 AutoMapper / Mapster。
3. **写回走显式 `SaveXxxInput`**：service 负责找实体、赋值、SaveChanges；VM→实体方向零映射。Input 同时用作 `GetForEdit(id)` 的返回类型（预填好），VM 只是它的 INPC 薄包装。
4. **变更事件由 service 统一发布**：写操作（Save/Copy/Delete）末尾发布 `XxxChangedEvent`，载荷 = 操作类型（Added/Updated/Removed）+ 变更后 DTO（Removed 只带 Id）。VM 在 `OnNavigatedTo` 拉初始数据并订阅，`OnNavigatedFrom` 退订。
5. **列表更新默认整条替换/插入/移除**；只有高频实时列表才为那一个列表单独上 INPC 项（ObservableObject），不做全局机制。
6. **插件实体的展示模型与映射由插件自带**；框架不为反射注册的插件派生实体提供通用映射机制。

## Considered Options

- **实体直绑 ViewModel**（现状做法）：弃用——实体是纯 POCO 无 INPC，编辑场景本就绑不了（RecipeContextViewModel 已被迫手写 DTO 佐证）；实体被跟踪时编辑无反悔权，取消/回滚是坑。
- **引入 AutoMapper / Mapster**：弃用——内置实体约 6 个，映射库省的代码抵不上黑盒与调试成本；查询投影的手写本来躲不掉。
- **Service 层投影契约类型**（选定）：查询投影在 EF 层完成（顺带省列），AsNoTracking 现状不变；DTO 无 INPC 恰合只读直绑；Repository/Service 抽象现成，映射有天然落点。

## Consequences

- **迁移序列（一次性，不留双轨）**：① 本文档 → ② RecipeContextViewModel 收尾（`GetForEdit`/`Save(Input)`，删手工回写）→ ③ RecipeHomeViewModel（复制配方变 `service.CopyRecipe(...)`，订阅事件）→ ④ LogViewerViewModel（分页只读，当前页内新增则 append，否则翻页自然重查）。
- 迁移触到的 `.Result` 同步阻塞、ServiceLocator 顺手改（await 化 / 构造注入），不做全局专项。
- .NET 项目引用是传递的："UI 看不见实体"靠约定 + review 守，不是编译器硬墙；将来需要硬约束时可加架构测试。
- 树外旧账，另行处理：`EnsureCreated` 与 Migrations 混用（`FrameworkAppBase.cs`）。
