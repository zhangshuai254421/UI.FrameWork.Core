# SemiControl WPF Framework

工业设备上位机 WPF 应用框架，Prism 9 + DryIoc + Serilog，多目标 `net8.0-windows;net10.0-windows`。

## Project Structure

```
01-UI框架/
├── UI.FrameWork/                  # 启动项目 (SemiAppliaction)
│   ├── App.xaml.cs                # 入口，注册模块
│   ├── UiModule.cs                # 业务模块 (MainView + ToolBoxView)
│   └── Views/                     # MainView, EmptyView
├── UI.FrameWork.Core/             # 框架核心库
│   ├── FrameworkAppBase.cs        # Prism 应用基类 (DI/Mutex/Login/Serilog)
│   ├── FrameworkModule.cs         # 核心模块 (注册 BaseView/Header/Footer)
│   ├── Main/
│   │   ├── ShellWindow.xaml       # 主窗口，定义 Region 布局
│   │   ├── View/BaseView.xaml     # 主区域 BaseView (BaseViewMainRegion + ToolBoxRegion)
│   │   ├── DirectView/ViewModel   # Direct 视图
│   │   ├── LogViewerView/ViewModel # 日志查看器 (分页 + 日期筛选)
│   │   ├── HeaderView/ViewModel   # 顶部状态栏
│   │   └── FooterView/ViewModel   # 底部工具栏 (后退/前进按钮)
│   └── Common/
│       └── PrismRegionNavigator.cs
├── SemiControl_Net/               # 自定义控件库 (SemiControl.dll)
│   ├── Controls/Input/            # DateTimePicker, ListClock, CalendarWithClock
│   ├── Controls/                  # IconButton, ToggleIconButton, BigNumericKeypad
│   ├── Themes/                    # 主题系统 (Skin.xaml 入口)
│   │   ├── Basic/                 # Colors, Brushes, Fonts, Geometries
│   │   └── Styles/                # 控件默认样式
│   └── Properties/AssemblyInfo.cs # XmlnsDefinition 映射
├── Framework.Core/                # 框架基础 (RegionNames, INavigationService, RouterHelper)
├── EFCore.Infrastructure/         # EF Core 基础设施 (EntityServiceBase, IUnitOfWork)
├── EFCore.Repository/             # 仓储接口 (IRepository, IEntityServiceBase, PageParameter)
├── Log.Domain/                    # 日志实体 + ISerilogService
└── Log.Infrastructure/            # 日志仓储实现
```

## Region Architecture

```
ShellWindow
├── MainRegion (ContentControl)          → BaseView
│   ├── BaseViewMainRegion (ContentControl) → MainView / LogViewerView / DirectView
│   └── ToolBoxRegion (ContentControl)      → NumberKeyPadView / DirectView
├── HeaderViewRegion (ContentControl)       → HeaderView
└── FooterRegion (ContentControl)           → FooterView
```

## Key Patterns

### Navigation Service (`Framework.Core.Common.NavigationService`)
- `NavigateToAsync(viewName, regionName?, parameters?)` — 默认导航到 `BaseViewMainRegion`
- `GoBack()` / `GoForward()` — 同步操作 BaseViewMainRegion 和 ToolBoxRegion 的 Journal
- `[ToolBoxFor(typeof(MainView))]` 特性自动建立主视图与工具栏的配对

### ViewModel Communication
- **EventAggregator**: 跨 ViewModel 解耦通信 (e.g., `LayoutModeChangedEvent`)
- **INavigationAware**: `OnNavigatedTo` / `OnNavigatedFrom` 中触发事件

### DI Registration
- `FrameworkAppBase.RegisterTypes()` — 全局依赖 (ILoggerFactory, INavigationService)
- `FrameworkModule.RegisterTypes()` — 注册 BaseViewModel + RegisterForNavigation
- `UiModule.RegisterTypes()` — 业务视图注册
- `IoC.Get<T>()` — 静态门面 (在 OnInitialized 中初始化)

### Module Load Order (matters!)
1. `FrameworkModule` 先加载 (注册 BaseView → MainRegion，子 Region 就绪)
2. `UiModule` 后加载 (导航 BaseViewMainRegion → MainView)

## Conventions
- ViewModel 继承 `BindableBase` (Prism)
- 命令使用 `DelegateCommand`
- XAML 控件前缀 `s:` → `https://github.io/semicontrol` (SemiControl_Net)
- 主题资源用 `DynamicResource` (PrimaryBrush, TextBrush, BorderBrush 等)
- 日志实体 `SerilogHistory`, 服务接口 `ISerilogService`

## Known Caveats
- `SemiControl_Net/Controls/Input/` 的文件在 csproj 中被 `Compile Remove` 排除，需要时去掉
- `WatermarkTextBox.cs` 需修复: `namespace SemiControl.Controls` 后缺 `{`
- 启动时 `FrameworkModule` 必须先于 `UiModule` 加载，否则 BaseViewMainRegion 不存在导致导航失败
- `INavigationService.GoBack()` 同步操作 BaseViewMainRegion + ToolBoxRegion 两个 Journal
