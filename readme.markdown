# SemiControl WPF 框架 — 项目分析

> 分析日期：2026-05-18

---

## 一、项目类型与技术栈

| 维度 | 详情 |
|------|------|
| **项目类型** | WPF 桌面应用程序（工业/半导体设备控制上位机） |
| **目标框架** | `net8.0-windows` / `net10.0-windows`（多目标） |
| **UI 框架** | WPF (XAML) |
| **MVVM 框架** | **Prism 9.0** (`Prism.Wpf` + `Prism.Container.DryIoc`) |
| **DI 容器** | DryIoc（通过 Prism 集成） |
| **日志框架** | **Serilog 4.3** + `Microsoft.Extensions.Logging` 抽象 |
| **语言** | C# 12 (ImplicitUsings, Nullable enabled) |

### NuGet 依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| `Prism.Wpf` | 9.0.537 | MVVM 框架、模块化、Region 导航 |
| `Prism.Container.DryIoc` | 9.0.107 | DryIoc DI 容器适配 |
| `Serilog` | 4.3.1 | 结构化日志核心 |
| `Serilog.Extensions.Logging` | 10.0.0 | 桥接 Microsoft.Extensions.Logging |
| `Serilog.Sinks.File` | 7.0.0 | 日志文件输出（按天滚动，保留10天） |

---

## 二、整体架构分层

项目由 **5 个子项目** 组成，呈典型的 **分层 + 模块化** 架构：

```
┌─────────────────────────────────────────────────────────┐
│          SemiAppliaction (UI.FrameWork)                  │  ← 启动项目 (WinExe)
│          App → FrameworkAppBase                           │
│          UiModule + FrameworkModule                       │
├─────────────────────────────────────────────────────────┤
│          UI.FrameWork.Core                                │  ← 框架核心库
│  FrameworkAppBase / FrameworkModule / IoC                 │
│  ShellWindow / LoginView / HeaderView / FooterView        │
│  PrismRegionNavigator                                     │
├────────────────────────────┬────────────────────────────┤
│     SemiControl_Net        │   SemiControl_Shard         │  ← 控件库层
│  (自定义 WPF 控件库)        │  (共享项目, 当前未启用)      │
│  IconButton / Toggle-      │  IconButton / Panel         │
│  IconButton / BigNum-      │                              │
│  Keypad / SimplePanel      │                              │
│  / TimeBlock / Attach      │                              │
├────────────────────────────┴────────────────────────────┤
│          SemiControlDemo_Net                              │  ← Demo 测试项目
└─────────────────────────────────────────────────────────┘
```

### 各层职责

| 层 | 项目 | 职责 |
|----|------|------|
| **应用层** | `UI.FrameWork` (SemiAppliaction) | 业务应用入口、注册业务模块和视图 |
| **框架核心层** | `UI.FrameWork.Core` | Prism 应用基类、模块化、IoC 工具、Region 导航、Shell/Login 窗口 |
| **控件库层** | `SemiControl_Net` | 自定义 WPF 控件、附加属性、值转换器、主题资源 |
| **共享层** | `SemiControl_Shard` | 跨项目共享代码（当前未启用，已被 csproj 注释） |
| **Demo 层** | `SemiControlDemo_Net` | 控件库独立演示程序 |

---

## 三、核心模块与接口

### 3.1 框架核心 (`UI.FrameWork.Core`)

| 类/模块 | 基类/接口 | 作用 |
|---------|-----------|------|
| `FrameworkAppBase` | `PrismApplicationBase` | 抽象应用基类：容器初始化(DryIoc)、Shell 创建、单实例 Mutex、登录流程、Serilog 日志初始化 |
| `FrameworkModule` | `IModule` | 注册 HeaderView→HeaderViewRegion、FooterView→FooterRegion、NumberKeyPadView 导航 |
| `IoC` | `static class` | 静态 IoC 门面，封装 `Get<T>(string key)` 便捷方法 |
| `PrismRegionNavigator` | `class` | Prism Region 异步导航帮助类，支持超时和统一错误处理 |
| `ShellWindow` | `Window` | 应用主 Shell，无边框窗口，定义 4 个 Region |
| `LoginView` | `Window` | 登录窗口，Dialog 模式 |
| `LoginViewModel` | `BindableBase` | 登录逻辑：用户名/密码绑定、LoginCommand |
| `HeaderView` | `UserControl` | 顶部状态栏（Logo、急停按钮、设备状态信息） |
| `FooterView` | `UserControl` | 底部工具栏（数字键盘、键盘、轴控制、Direct 等 ToggleIconButton） |
| `NumberKeyPadView` | `UserControl` | 数字键盘视图 |
| `TimeBlock` | `UserControl` | 实时时钟控件（200ms DispatcherTimer 刷新） |

#### ShellWindow Region 布局

| Region 名称 | 位置 | 用途 |
|-------------|------|------|
| `HeaderViewRegion` | 顶部 (Row 0) | 状态栏/头部 |
| `MainRegion` | 中部 (Row 2) | 主内容区 |
| `ToolBoxRegion` | 底部左侧 | 工具箱 |
| `FooterRegion` | 底部右侧 | 页脚工具按钮 |

### 3.2 自定义控件库 (`SemiControl_Net`)

#### 控件

| 控件 | 基类 | 核心依赖属性 |
|------|------|-------------|
| `IconButton` | `Button` | `IconGeometry`, `IconWidth`, `IconHeight`, `IconText`, `TopLeftContent`, `IsNeedRedMark` |
| `ToggleIconButton` | `ToggleButton` | `IconGeometry`, `IconWidth`, `IconHeight`, `TopLeftContent`, `StatusBrush` |
| `BigNumericKeypad` | `Control` | 完整虚拟键盘，内置键码映射表（支持 Ctrl/Alt/Shift 修饰键、字母/数字/符号） |
| `SimplePanel` | `Panel` | 轻量级面板，所有子元素重叠放置，`MeasureOverride` 取子元素最大宽高 |

#### 附加属性 (Attached Properties)

| 类 | 属性 | 类型 | 用途 |
|----|------|------|------|
| `ButtonAttach` | `IconGeometory` | `Geometry` | 为任意按钮附加图标几何图形 |
| `BorderElement` | `CornerRadius` | `CornerRadius` | 附加圆角（支持继承） |
| `BorderElement` | `Circular` | `bool` | 设为 true 时自动计算半圆形圆角（绑定 ActualWidth/Height） |
| `IconElement` | `Geometry` | `Geometry` | 通用图标几何图形 |
| `IconElement` | `Source` | `ImageSource` | 通用图标图像源 |
| `IconElement` | `Width` / `Height` | `double` | 通用图标尺寸 |

#### 工具类

| 类 | 接口 | 作用 |
|----|------|------|
| `BorderCircularConverter` | `IMultiValueConverter` | 根据 ActualWidth/Height 自动计算半圆圆角 |
| `ValueBoxes` | `static class` | 预装箱值类型缓存（bool/double/int/Orientation/Visibility），减少 GC 压力 |

### 3.3 主题系统 (`Themes/`)

```
Themes/
├── Skin.xaml                    ← 主题入口，合并所有资源字典
├── IconGeometry.xaml            ← 图标几何图形（STOP/START/ENTER/EXIT 及主菜单图标）
├── Basic/
│   ├── Colors.xaml              ← 色彩体系（30+ 语义化颜色键）
│   ├── Brushes.xaml             ← 画刷定义
│   ├── Brushes/ButtonBrushes.xaml
│   ├── Fonts.xaml               ← 字体资源
│   ├── Geometries.xaml          ← 几何图形资源
│   └── Size.xaml                ← 尺寸常量
└── Styles/
    ├── Base/BaseStyle.xaml
    ├── Button.xaml              ← Button 默认样式
    ├── IconButton.xaml          ← IconButton 默认样式
    ├── ToggleButton.xaml        ← ToggleIconButton 默认样式
    └── BigNumericKeypad.xaml    ← BigNumericKeypad 默认样式
```

#### 设计令牌（Design Tokens）

色彩体系按语义分组：

| 分组 | 示例键 |
|------|--------|
| **主色 (Primary)** | `PrimaryColor` / `PrimaryDeepColor` / `PrimaryLightColor` / `PrimaryDefaultColor` / `PrimaryControlToolColor` |
| **辅色 (Secondary)** | `SecondaryDeepColor` / `SecondaryColor` / `SecondaryLightColor` |
| **背景 (Background)** | `BackgroundDeepColor` / `BackgroundColor` / `BackgroundLightColor` |
| **第三色 (Thirdly)** | `ThirdlyDeepColor` / `ThirdlyColor` / `ThirdlyLightColor` |
| **边框 (Border)** | `PrimaryBorderColor` / `BorderColor` / `SecondaryBorderColor` |
| **文字 (Text)** | `SpecialTextColor` / `PrimaryTextColor` / `TextColor` / `SecondaryTextColor` / `DisableTextColor` / `MenuTextColor` |
| **交互** | `HoverColor` |
| **语义** | `WarningBrush`（警告/急停） |

### 3.4 应用启动流程

```
App.ConfigureModuleCatalog()
  ├── UiModule → MainView 注册到 MainRegion
  └── FrameworkModule
        ├── HeaderView → HeaderViewRegion
        ├── FooterView  → FooterRegion
        └── NumberKeyPadView (导航注册)

App.OnInitialized()
  ├── IoC.GetInstance = Container.Resolve
  ├── Mutex("title") 单实例检测 → 已有实例则激活并退出
  ├── LoginView.ShowDialog() → 登录验证
  │     ├── result == false → base.OnInitialized() → ShellWindow 显示
  │     └── result == true  → Shutdown()
  └── Serilog 日志初始化（文件滚动，Debug 级别）
```

---

## 四、总结

这是一个典型的 **工业设备上位机 WPF 应用框架**，采用成熟的 **Prism + DryIoc 模块化架构**，核心特性包括：

- ✅ 自定义控件库（图标按钮、虚拟键盘、轻量面板等工业常用控件）
- ✅ 完整的设计令牌/主题系统（30+ 语义化颜色、画刷、字体、尺寸）
- ✅ Prism Region 区域导航 + 模块化加载
- ✅ 登录鉴权流程
- ✅ 结构化日志（Serilog + 微软日志抽象）
- ✅ 单实例保护（Mutex）
- ✅ 多目标框架支持（net8.0 + net10.0）

> **当前状态**：框架基础已搭建完成，业务模块（MainView 中的 F1-F8 功能按钮）尚在开发中，大部分已被注释。
