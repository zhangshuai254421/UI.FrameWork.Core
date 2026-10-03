using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Framework.Alarm;

namespace SemiAppliaction.Views.Tests
{
    /// <summary>
    /// 报警测试页渲染联测（两清单一直空的排查钉子，第二层）：数据层直通已钉死（AlarmDemoRigTests），
    /// 这一层把 <see cref="AlarmTestView"/> 真身放进 Window 跑完整布局——
    /// 绑定送达没有（ItemsSource）、容器生成没有（ItemContainerGenerator）、异常吞没没有（绑定侦听 + 未捕获钩子）
    /// 一层一个断言，断点在哪一层当场现形。STA 线程手搭（测试项目没有 STA 包）。
    /// </summary>
    public class AlarmTestViewRenderingTests
    {
        /// <summary>真装置直通渲染：引擎升了、VM 接住了、ListView 也得把行渲染出来。</summary>
        [Fact]
        public void 页面真身_剧本六拍_主列表渲染三行()
        {
            var report = RunOnSta(() =>
            {
                // 与应用同款资源环境（App.xaml 的合并字典）：ListView 吃的是 HandyControl 的隐式样式
                if (Application.Current is null)
                {
                    _ = new Application();
                }

                foreach (var uri in new[]
                         {
                             "pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml",
                             "pack://application:,,,/HandyControl;component/Themes/Theme.xaml",
                             "pack://application:,,,/SemiControl;component/Themes/Skin.xaml",
                             "/SemiAppliaction;component/Resources/Themes/GlobalStyle.xaml",
                             "/UI.FrameWork.Core;component/Main/View/Dictionary1.xaml",
                         })
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.RelativeOrAbsolute) });
                }

                // 数据层先行：与 AlarmDemoRigTests 同款直通——先证明 MainRows 里有货
                var rig = new AlarmDemoRig(new InMemoryAlarmDefinitionStore());
                var ui = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
                var list = new AlarmListViewModel(rig.Engine, rig.Store, ui);
                var dc = new PageStub(list);
                for (var beat = 0; beat < 6; beat++)
                {
                    rig.PumpNext();
                    list.RefreshFilters();
                }

                var errors = AttachBindingTrace();
                Exception? unhandled = null;
                Dispatcher.CurrentDispatcher.UnhandledException += (_, e) =>
                {
                    unhandled = e.Exception;
                    e.Handled = true;
                };
                var probeResources = $"资源探测：ListView隐式={(Application.Current.Resources[typeof(ListView)] is Style s1 ? s1.TargetType.Name : "无")} ListViewItem隐式={(Application.Current.Resources[typeof(ListViewItem)] is Style s2 ? s2.TargetType.Name : "无")}";

                var view = new AlarmTestView { DataContext = dc };
                // 对照探针：同一集合，Style={x:Null}（WPF 默认模板）vs 隐式样式（HandyControl 接管）
                var probeDefault = new ListView { ItemsSource = list.MainRows, Style = null };
                var probeImplicit = new ListView { ItemsSource = list.MainRows };
                var probes = new StackPanel();
                probes.Children.Add(probeDefault);
                probes.Children.Add(probeImplicit);
                var win = new Window
                {
                    Width = 1200,
                    Height = 800,
                    Content = new DockPanel
                    {
                        Children = { view, probes },
                    },
                };
                win.Show();
                Pump(5); // 布局 + 渲染队列：容器生成发生在 ItemsPresenter 的 Measure 里

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"MainRows.Count={list.MainRows.Count}");
                sb.AppendLine(probeResources);
                if (DescendantsOfType<ListView>(view).Count() == 0)
                {
                    // 可视树快照：ListView 没出世时看树里到底长成了什么样
                    sb.AppendLine("可视树：" + Describe(view));
                }
                if (unhandled is not null)
                {
                    sb.AppendLine($"未捕获异常：{unhandled.GetType().Name}: {unhandled.Message}");
                    if (unhandled.InnerException is { } inner)
                    {
                        sb.AppendLine($"内层：{inner.GetType().Name}: {inner.Message}");
                        sb.AppendLine(inner.StackTrace);
                    }
                    sb.AppendLine(unhandled.StackTrace);
                }

                var lists = DescendantsOfType<ListView>(view).ToList();
                sb.AppendLine($"ListView 数={lists.Count}");
                foreach (var lv in lists)
                {
                    var source = lv.ItemsSource as IEnumerable;
                    var count = source?.Cast<object>().Count() ?? -1;
                    sb.AppendLine($"ListView ItemsSource={(lv.ItemsSource?.GetType().Name ?? "null")} count={count} Items.Count={lv.Items.Count}");
                    var presenters = DescendantsOfType<ItemsPresenter>(lv).Count();
                    var style = lv.Style;
                    var applied = lv.ApplyTemplate();
                    presenters = DescendantsOfType<ItemsPresenter>(lv).Count();
                    sb.AppendLine($"  模板={(lv.Template is null ? "null" : lv.Template.ToString())} Style.TargetType={(style?.TargetType?.Name ?? "null")} ApplyTemplate={applied} ItemsPresenter数={presenters} 生成器={lv.ItemContainerGenerator.Status}");
                    sb.AppendLine($"  ItemsPanel={lv.ItemsPanel?.ToString()} ItemContainerStyle={(lv.ItemContainerStyle is null ? "null" : lv.ItemContainerStyle.ToString())}");
                    var presenter = DescendantsOfType<ItemsPresenter>(lv).FirstOrDefault();
                    if (presenter is not null)
                    {
                        sb.AppendLine($"  Presenter 尺寸={presenter.ActualWidth}x{presenter.ActualHeight} 可视={presenter.IsVisible}");
                    }
                    else
                    {
                        sb.AppendLine("  Presenter 无 → 模板树：" + Describe(lv));
                    }

                    var container = lv.ItemContainerGenerator.ContainerFromIndex(0);
                    sb.AppendLine($"  容器[0]={container?.GetType().Name ?? "null"} 容器Style={(container is ListViewItem lvi ? lvi.Style?.TargetType?.Name ?? "null" : "-")}");
                    if (container is not null)
                    {
                        var texts = DescendantsOfType<TextBlock>(container)
                            .Select(t => t.Text)
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Take(8);
                        sb.AppendLine($"  行内文本：{string.Join(" | ", texts)}");
                    }
                }

                foreach (var (name, probe) in new[] { ("Style=x:Null", probeDefault), ("隐式样式", probeImplicit) })
                {
                    var container0 = probe.ItemContainerGenerator.ContainerFromIndex(0);
                    sb.AppendLine($"探针[{name}]：容器[0]={container0?.GetType().Name ?? "null"} ItemsPresenter数={DescendantsOfType<ItemsPresenter>(probe).Count()} 模板套用={probe.ApplyTemplate()}");
                }

                // 解剖隐式样式的模板：里面到底有没有 ItemsPresenter
                if (probeImplicit.Template is not null)
                {
                    var xaml = System.Windows.Markup.XamlWriter.Save(probeImplicit.Template);
                    sb.AppendLine("---- 隐式 ListView 模板 XAML ----");
                    sb.AppendLine(xaml.Length > 4000 ? xaml[..4000] + "…(截断)" : xaml);
                }

                sb.AppendLine("---- 绑定错误 ----");
                foreach (var line in errors)
                {
                    sb.AppendLine(line);
                }

                win.Close();
                return sb.ToString();
            });

            // 钉子：数据层有货 + 绑定送达 + 容器生成 + 行内见位号文本，四层全绿才算渲染通
            // （零间隔连打六拍只有憋压即时档升——升温的 2 秒延时靠采样时间戳，连打永不够）
            Assert.Contains("MainRows.Count=1", report);
            Assert.Contains("ItemsSource=ObservableCollection`1 count=1", report);
            Assert.DoesNotContain("ListView ItemsSource=null", report);
            Assert.Contains("容器[0]=ListViewItem", report);
            Assert.Contains(AlarmDemoRig.PressTag, report);
            Assert.DoesNotContain("未捕获异常", report);
        }

        /// <summary>替身页面 VM：只供绑定路径探针（真 AlarmTestViewModel 依赖 IoC，此处不涉）。</summary>
        private sealed class PageStub
        {
            public PageStub(AlarmListViewModel list) => List = list;

            public AlarmListViewModel List { get; }

            public string StatusText => "测试中";
        }

        /// <summary>挂上 WPF 绑定错误侦听（TraceSource「System.Windows.Data」），收走得着的行。</summary>
        private static List<string> AttachBindingTrace()
        {
            var lines = new List<string>();
            var source = PresentationTraceSources.DataBindingSource;
            source.Switch.Level = SourceLevels.Warning;
            source.Listeners.Add(new CapturingListener(lines));
            return lines;
        }

        /// <summary>把错误收进列表的侦听器。</summary>
        private sealed class CapturingListener : TraceListener
        {
            private readonly List<string> _lines;

            public CapturingListener(List<string> lines) => _lines = lines;

            public override void Write(string? message) => _lines.Add(message ?? string.Empty);

            public override void WriteLine(string? message) => _lines.Add(message ?? string.Empty);
        }

        /// <summary>STA 线程执行体：装配、跑、掏可视树全在 UI 线程上，结果串回测试线程。</summary>
        private static string RunOnSta(Func<string> body)
        {
            string? report = null;
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    report = body();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA 线程 60 秒未归");
            if (failure is not null)
            {
                throw failure;
            }

            return report ?? string.Empty;
        }

        /// <summary>泵几帧：后台优先级空转一帧，布局/渲染队列随之走完。</summary>
        private static void Pump(int frames)
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            for (var i = 0; i < frames; i++)
            {
                var frame = new DispatcherFrame();
                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }

        /// <summary>可视树快照：逐层打印节点类型（排查模板没套上/内容没进树）。</summary>
        private static string Describe(DependencyObject node, int depth = 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(new string(' ', depth * 2));
            var name = node is FrameworkElement fe ? $"{fe.GetType().Name}({fe.Name})" : node.GetType().Name;
            sb.AppendLine(name);
            var count = VisualTreeHelper.GetChildrenCount(node);
            for (var i = 0; i < count; i++)
            {
                sb.Append(Describe(VisualTreeHelper.GetChild(node, i), depth + 1));
            }

            return sb.ToString();
        }

        /// <summary>可视树深挖：收指定类型的后代。</summary>
        private static IEnumerable<T> DescendantsOfType<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var deep in DescendantsOfType<T>(child))
                {
                    yield return deep;
                }
            }
        }
    }
}
