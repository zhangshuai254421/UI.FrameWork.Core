namespace PrismUI.Core
{
    /// <summary>
    /// 视图名称常量集中定义，用于区域导航，名称与属性名保持一致。
    /// </summary>
    public static class ViewNames
    {

        #region 公共平台的配方系统

        /// <summary>日志查看视图。</summary>
        public static string LogViewerView => nameof(LogViewerView);
        #endregion
        /// <summary>数字键盘视图。</summary>
        public static string NumberKeyPadView => nameof(NumberKeyPadView);
        /// <summary>字符串键盘视图。</summary>
        public static string StringKeyPadView => nameof(StringKeyPadView);
        /// <summary>轴控制视图。</summary>
        public static string AxisView => nameof(AxisView);
        /// <summary>直接控制视图。</summary>
        public static string DirectView => nameof(DirectView);
        /// <summary>主视图。</summary>
        public static string MainView => nameof(MainView);

        /// <summary>默认视图。</summary>
        public const string DefaultView = nameof(DefaultView);
        /// <summary>空白占位视图。</summary>
        public const string EmptyView = nameof(EmptyView);
        /// <summary>空白占位视图（第二块区域用）。</summary>
        public const string EmptyView2 = nameof(EmptyView2);

        /// <summary>配方上下文视图。</summary>
        public const string RecipeContextView = nameof(RecipeContextView);

        /// <summary>报警列表页（报警家族注册同名视图；壳只认名字，不引用家族程序集）。</summary>
        public const string AlarmListView = nameof(AlarmListView);
    }

}
