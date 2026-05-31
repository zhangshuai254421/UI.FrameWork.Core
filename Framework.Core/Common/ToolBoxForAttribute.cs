using System;

namespace Framework.Core.Common
{
    /// <summary>
    /// 标记一个 View 作为指定 MainView 在 ToolBoxRegion 中的配对工具栏视图。
    /// NavigationService 会自动扫描此特性，导航 BaseViewMainRegion 时联动导航 ToolBoxRegion。
    ///
    /// 用法：
    /// <code>
    /// [ToolBoxFor(typeof(LogViewerView))]
    /// public partial class LogToolBoxView : UserControl { ... }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ToolBoxForAttribute : Attribute
    {
        /// <summary>
        /// 配对的主视图类型（将被导航到 BaseViewMainRegion 的 View）
        /// </summary>
        public Type MainViewType { get; }

        public ToolBoxForAttribute(Type mainViewType)
        {
            MainViewType = mainViewType ?? throw new ArgumentNullException(nameof(mainViewType));
        }
    }
}