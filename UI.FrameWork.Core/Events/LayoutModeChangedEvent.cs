using Prism.Events;

namespace UI.FrameWork.Core.Events
{
    /// <summary>
    /// 布局模式切换事件。true=全屏（隐藏工具栏），false=普通（显示工具栏）
    /// </summary>
    public class LayoutModeChangedEvent : PubSubEvent<bool>
    {
    }
}