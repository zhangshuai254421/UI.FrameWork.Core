namespace PrismUI.Core;

/// <summary>
/// 配方切换事件：发布当前选中的配方名称（字符串）。
/// </summary>
public class ChangeRecipeEvent : PubSubEvent<string>
{
}