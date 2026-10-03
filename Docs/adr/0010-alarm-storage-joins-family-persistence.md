# 报警存储并入家族持久化体系：EF Core 实现接缝，报警家族两件套落地

Status: accepted（翻案 ADR-0009 的存储选型与家族判据两处，其余规则继续有效）

核心原则一句话：**存储接缝不变**——引擎仍只认 `IAlarmDefinitionStore` / `IEventStore`；接缝背后的实现由裸 SQLite 换成 EF Core（SQLite provider），报警家族按 ADR-0008 形态补齐 `.Domain` + `.Infrastructure` 两件套（`.UI` 仍无，呈现归宿主）。

## 背景

ADR-0009 拍板 v1「存储直接 SQLite（Microsoft.Data.Sqlite）」，并以「家族判据撑不起三件套」弃用了模块家族形态。竖切工单 #11/#13 以裸 SQLite 跑通后，两实现合计 429 行手写建表、参数装配与类型转换；而事件历史 90 天保留期（哲学 §8.1）要跨版本存活——手写 schema 没有迁移机制，每次改表都是一次手工数据搬家。工单 #19 规格评审拍板并入全库既有的家族持久化体系（Recipe / Log / Device 三家族同款），#20/#21/#22 执行完毕：实体与存储契约搬家入 `.Domain`，EF 实现过同一套契约测试，宿主建库一口与演示装置真库通电。

## 翻案内容（对 ADR-0009）

1. **规则 5 翻案——「直接 SQLite」→「EF Core 实现接缝」**：裸 SQLite 两实现（`SqliteAlarmDefinitionStore` / `SqliteEventStore`，429 行）与 Microsoft.Data.Sqlite 直用依赖退役；`Framework.Alarm.Infrastructure` 落地 EF 实现，契约测试原封不动换实现再跑过关。翻的是接缝背后的实现，不是接缝本身——「引擎不直连数据库」从规则 5 原样存活。
2. **家族判据翻案——「三件套弃用」→「两件套成立，`.UI` 仍无」**：当时的判据「v1 无多存储、多 UI 需求」对 UI 一半仍然成立（呈现归宿主，演示页在宿主 `UI.FrameWork\Views\` 与 DetectionTestView 同层，ADR-0009 规则 1 的实质——UI 不进能力库——不动）；对存储一半被推翻：需要的是**跨版本迁移**而非多存储，而迁移正是家族 `.Infrastructure` 形态的标配能力。报警家族自此与 Recipe / Log / Device 同形。

## 规则

1. **存储接缝是唯一存续面**：`IAlarmDefinitionStore` / `IEventStore`（住 `Framework.Alarm.Domain\Service\`）形状不变；引擎与宿主只认接缝，EF 是可替换的实现细节。契约测试（`AlarmDefinitionStoreContract` / `EventStoreContract`）是接缝价值的实证——InMemory 与 EF 两实现共吃同一套断言。
2. **家族两件套照 ADR-0008 铁律**：`.Domain` 装实体 + 存储契约，零 EF 引用（连 `EFCore.Repository` 也未挂——实体基类暂未用到），归属特性走字符串形态指名 `Framework.Alarm.Infrastructure.DataContext`（分层禁引用，typeof 会成环）；`.Infrastructure` 装上下文、TPH 映射、Migrations，是家族内唯一知道数据库的地方。
3. **建库归宿主一口**：`AlarmDatabase.Initialize`（挖目录 → `Migrate` 建表 → WAL 就位），宿主与三库 EnsureCreated 同刻调用；存储构造不建库。走迁移不走 EnsureCreated——90 天历史要跨版本存活，EnsureCreated 建出的库套不上迁移。
4. **连接模型**：两存储各开连接打同一库文件（连接字符串选项，不共享连接实例）；WAL 读写不互堵（UI 查详情读 vs 引擎落库写），写写相撞由 Microsoft.Data.Sqlite 命令超时（默认 30 秒）busy 重试兜底。
5. **裸 SQLite 不回潮**：Microsoft.Data.Sqlite 只以 EF provider（经 `EFCore.Infrastructure` 传递引用）与建库口连接串解析的身份存在；手写 schema / 参数装配 / 类型转换不再出现在报警家族。

## Considered Options

- **维持裸 SQLite，补手写迁移脚本**：弃用——为一张事件表复刻 EF 迁移机制，家族既有形态不用，自造第二套持久化惯例。
- **内存 + JSONL**（哲学草案旧推荐，§12 #4 已否决过一次）：弃用——连 SQLite 都不落，90 天保留期与 KPI 回放无从谈起。
- **EF Core 家族两件套（选定）**：迁移、TPH、跟踪纪律白拿；与全库家族形态一致，接缝契约测试保证实现可替换。

## Consequences

- 新增工程 `Framework.Alarm.Domain` / `Framework.Alarm.Infrastructure` 入 sln；`Framework.Alarm` 引擎改引 `.Domain`，自身不再有任何存储实现与 SQLite 依赖（零 Prism/WPF 照旧）。
- 事件表 TPH 一表收六种事件：判别列 Kind=类型名（映射表一份，实体配置与回放查询点名共吃），子类专属列按语义并同列（期限并 `ShelvedUntil`、操作员并 `Operator`），时刻一律 O 格式 TEXT（`StorageConverters` 一处写三处用）。
- 跟踪纪律由实现保证而非调用方自律：落库即清（`ChangeTracker.Clear`），读回一律 `AsNoTracking` 交游离副本——append-only 本子不清理则跟踪器只增不减。
- ADR-0009 其余规则全部继续有效：判定权只住引擎层、档案驱动两条沉默决策、时钟接缝、三级优先级、KPI 先文档后代码、旧皮肤 v2 桥接；规则 9 的非法转换契约已随 #9 状态机 TDD 落定（静默忽略）。
- 哲学文档（`Docs/alarm-philosophy.md`）存储表述与悬置项注记随本 ADR 同步修订为 v1.1（#23）。
