# 分层命名与引用方向：Domain 纯净，箭头指向中心

Status: accepted

核心原则一句话：每个业务家族统一为 `域词.Domain`（纯模型）+ `域词.Infrastructure`（持久化）+ 可选适配器/UI；所有引用箭头从细节指向中心，后缀即角色，家族三名（目录 / 工程 / 命名空间根）一致。

## 规则

1. **角色后缀白名单**：
   - **`<域词>.Domain`**：实体 + 实体服务接口。**只准引用共享内核 `EFCore.Repository` 与 `Framework.Core` 两个工程**（前者给实体基类与服务接口约定，后者给 `DefaultDbContext` 归属特性）；铁律——Domain 里出现 `Microsoft.EntityFrameworkCore`（using 或 csproj 任一处）即违约，引用任何家族内工程即违约。
   - **`<域词>.Infrastructure`**：`DbContext`、`IEntityTypeConfiguration`、`Migrations`。引用本家族 Domain + `EFCore.Infrastructure` + 直接使用的 `Framework.Core`；家族内唯一知道数据库的地方。
   - **厂商适配器**（`Framework.Device.<厂商>`）：引用抽象 `Framework.Device`（ADR-0001 已定）。
   - **能力库无后缀**（`Framework.Imaging` / `Framework.Detection`）：零 Prism / WPF。
2. **家族三名一致**：目录名 = 工程名 = 命名空间根。反例即债务：原 `Log.Domain` 目录装着 `Serilog.Domain.csproj`（命名空间又是第三个名字），本次已统一为 `Log.*`。
3. **归属特性跟实体走**：`AssemblyAttributeDbContextResolver`（Framework.Core）在仓储构造时按**实体所在程序集**反查 `[assembly: DefaultDbContext]`，所以特性必须与实体同程序集，两种形态：
   - **typeof 形态**：实体与 DbContext 同程序集（Device 家族：实体与上下文都在 `.Infrastructure`）；或实体程序集可以合法引用它（插件程序集，如 UI 模块的参数实体指名宿主库）。
   - **字符串形态**：实体程序集禁止引用 DbContext 程序集时（Domain → Infrastructure 会成环），写 `[assembly: DefaultDbContext("Recipe.Infrastructure.DataContext, Recipe.Infrastructure")]`，运行时按名解析。
   - `.Infrastructure` 自己不放这个特性——实体不在那儿，正向扫描也会跳过本程序集，放了纯噪音。
4. **撞名自保两条**：域词与类型同名时（`Recipe` 既是命名空间根又是实体类），`.Infrastructure` 内引用该实体一律 `global::Recipe.Domain.Recipe` 全名；程序集级特性位于 namespace 声明之前看不见命名空间内类型，`typeof()` 必须写全名。

## Considered Options

- **保持 Device 家族原状**（无后缀中心 + `.Domain` 做持久化）：弃用——`.Domain` 一词两义，Recipe 的 Domain 是模型、Device 的 Domain 是存储，规则无法陈述。
- **全库向 Recipe 原状看齐**（`.Domain` 含 EF 配置）：弃用——Domain 拖着 EF 包，能力库、适配器、测试引 Domain 时被迫传递引用持久化依赖，"模型"名存实亡。
- **最小重命名 + 拆纯（选定）**：Device 只改后缀（`Framework.Device.Domain` → `Framework.Device.Infrastructure`，实体语义本来就是持久化）；Recipe/Log 把 EF 配置与 DbContext 搬进既有 `.Infrastructure` 工程；三家族从此一套话术。

## Consequences

- **EF 迁移搬家安全**：Migrations 与 `DbContext` 同程序集整体迁移（默认 MigrationsAssembly 约定），迁移历史表按迁移 ID 追踪、不按程序集；实体 CLR 全名不变，模型快照中的实体名字符串无需改动。
- **共享内核已知遗留**：`EFCore.Repository` 工程内含两个命名空间（`EFCore.Repository` 主体 + `EFCore.IRepository` 接口层）。命名空间统一会波及全库 using，留待独立小决策；目录名 = 工程名已对齐。
- **DI 反查是编译门照不到的暗区**：`Repository<TEntity>` 构造时才按实体程序集反查 DbContext（本 ADR 规则 3），编译通过不代表启动能过——改归属机制后必须实机启动一次验证。
- **类名保留 Serilog 词**（`SerilogService` / `ISerilogService` / `SerilogHistory`）：类名诚实描述实现（Serilog 后端），家族前缀（目录/工程/命名空间）统一为 `Log.*`——厂商名住实现类，不住家族名。
- **插件实体归属仍由扫描发现**：`DataContext.OnModelCreating` 扫描扩展程序集收集 `[DefaultDbContext]` 归属声明与派生实体兜底，机制未变；变的是归属声明写在**实体所在程序集**（规则 3），不再随 `DbContext` 走。
