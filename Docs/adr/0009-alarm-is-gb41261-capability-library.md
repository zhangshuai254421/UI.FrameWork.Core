# 报警是遵循 GB/T 41261 的顶层纯能力库：Framework.Alarm

Status: accepted——存储选型与家族判据两处已被 ADR-0010 翻案（见规则 5 与 Considered Options 的标注），其余规则继续有效

核心原则一句话：报警判定是一个独立能力库（Framework.Alarm，零 Prism/WPF 依赖），**判定权只住引擎层**——引擎读档案（`AlarmDefinition`）对条件事实做滤波与四状态流转，产出 `AlarmEvent` 流与活跃列表；设计依据是《报警哲学》（`Docs/alarm-philosophy.md` v1.0），规范依据是 GB/T 41261-2022《过程工业报警系统管理》（IDT IEC 62682:2014）。

## 背景

上位机现有报警形态是 HeaderView 的一层主题皮肤（`Infos.*` 绑定，全仓库无 C# 契约定义），无引擎、无状态机、无历史。团队完成 GB/T 41261-2022 报警管理课程学习并沉淀规范摘要（`Docs/research/0001-gb41261-alarm-standard.md`）后，决定自研报警模块按国标落地；报警判据、分级、存储等决策已拍板（拍板记录见报警哲学 §12）。

## 规则

1. **顶层能力库而非模块**：新建顶层 `Framework.Alarm`，仿 Framework.Imaging / Framework.Detection 模式——零 Prism/WPF、无页面无导航；宿主反挂能力库，引用单向。v1 的 UI 呈现为竖切演示页 AlarmTestView（进 UiModule，照抄 DetectionTestView 动线：RegisterForNavigation → 主页「测试」加按钮 → INavigationService 导航），不进能力库。
2. **判定权只住引擎层**：采集层只产出条件事实，不懂得「报警」二字；引擎读档案 → 死区/延时滤波（HysteresisEvaluator）→ 四状态机（四状态五转换，**RTN 只从「未确认」进入**，已确认后恢复直接回 Normal）；表现层只呈现引擎产出，不做任何判定——不比较数值、不重新过滤、不改限值，三态样式绑 State。
3. **档案驱动**：每条报警有 MADB 档案（`AlarmDefinition`：位号/描述/后果/原因/操作动作/MTR/严重度/优先级坐标/组态参数/评审留痕）。引擎两条沉默决策：**无档案不报警**、**报警中不重播**（Raised 只发一次）。
4. **优先级固定三级**（高/中/低，`Priority` 枚举），查严重度 × 最大响应时间矩阵得出、坐标记入档案；**锁存 v1 不做**。
5. **存储直接 SQLite**【已翻案 → ADR-0010：实现换 EF Core 家族形态，存储接缝与 SQLite 引擎不变，原文存照】：档案与事件历史 v1 直接 SQLite（Microsoft.Data.Sqlite），经存储接缝访问（`IAlarmDefinitionStore` / `IEventStore` 形状见摘要 §8.4、§8.11），引擎不直连数据库。事件历史 **append-only**，含带搁置标的事件全量落库，保留期 90 天。搁置 = 事件打标（`ShelvedUntil`），**不拦状态机**。
6. **时间逻辑走时钟接缝**：延时、驻留、颤抖判定依赖 `IAlarmClock`，禁止直连 `DateTime.Now`——凡是依赖时间的逻辑都要能拨表测试。
7. **KPI 目标先文档后代码**：七项指标与 EEMUA 基准默认值已写入报警哲学 §8；`KpiService` 落地是 v2 工单，v1 只要求事件历史库把七项指标的数据源存全。
8. **旧报警皮肤不接新引擎**：HeaderView 的 `Infos.*` 是宿主主题皮肤；桥接走适配器，v2 话题。
9. **非法状态转换的契约**（抛异常 or 静默忽略）是有意悬置的设计决策：随状态机 TDD 首个工单写测试定契约再实现。

## Considered Options

- **模块家族三件套**（Framework.Alarm.Domain / .Infrastructure / .UI，仿 Recipe）：弃用【家族判据已被 ADR-0010 部分翻案：.Domain + .Infrastructure 两件套成立，.UI 仍无】——报警引擎是纯判定逻辑，v1 无多存储、多 UI 需求，按 ADR-0008 的家族判据撑不起三件套；先能力库，将来需要时再升级。
- **只做事件记录、挂现有皮肤**（最小改动）：弃用——没有判定权的引擎就没有国标语义，皮肤照旧刷屏。
- **顶层纯能力库 + 宿主演示页（选定）**：与 Detection 同构，测试页与产线报警共用同一入口。

## Consequences

- 新增工程 `Framework.Alarm` 与 `Framework.Alarm.Tests`（xunit 三件套），入 sln。
- 能力库可引第三方实现库：Microsoft.Data.Sqlite，与 Detection 引 YoloDotNet 同理——引的是实现不是 UI。
- v1 不含 KpiService、锁存、Header 皮肤适配器——都在 v2。
- 宿主使用规则（判据、分级、搁置权限、保留期）见报警哲学；工艺相关的分档数值与限值由宿主自写工艺哲学。
- 状态机是第一块实现材料（课程与摘要共同指认）：TDD 从四状态五转换起步，确认/RTN 两路径测试分开写。
