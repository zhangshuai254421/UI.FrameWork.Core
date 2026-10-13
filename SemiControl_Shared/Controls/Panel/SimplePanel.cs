using System;
using System.Windows;
using System.Windows.Controls;


namespace SemiControl.Controls
{
    /// <summary>
    ///     轻量级面板，作为在不需要行/列分割等功能时替代 <see cref="Grid"/> 的选项。
    /// </summary>
    /// <remarks>
    ///     - 本面板会在测量阶段遍历所有子元素并使用子元素的期望大小来计算自己需要的空间，
    ///       返回的大小是所有子元素期望宽度与期望高度的最大值。
    ///     - 在排列阶段，面板会将自身可用的整个区域传递给每一个子元素，
    ///       因此所有子元素会被重叠放置在同一位置上。
    ///     - 若需要对子元素进行更复杂的排列（例如栈式、均分或网格），请使用对应的面板类型。
    /// </remarks>
    public class SimplePanel : Panel
    {
        /// <summary>
        ///     测量面板所需的大小。返回值的宽和高分别是所有子元素 DesiredSize 的最大宽度和最大高度。
        /// </summary>
        /// <param name="constraint">父容器给予的可用大小约束。</param>
        /// <returns>面板所需的大小（宽度 = 子元素最大期望宽度， 高度 = 子元素最大期望高度）。</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // 记录所有子元素中最大的宽和高
            var maxSize = new Size();

            foreach (UIElement child in InternalChildren)
            {
                if (child != null)
                {
                    // 在相同约束下测量每个子元素
                    child.Measure(constraint);

                    // 使用子元素的 DesiredSize 更新最大宽高
                    maxSize.Width = Math.Max(maxSize.Width, child.DesiredSize.Width);
                    maxSize.Height = Math.Max(maxSize.Height, child.DesiredSize.Height);
                }
            }

            return maxSize;
        }

        /// <summary>
        ///     将每个子元素排列到面板分配到的整个区域。子元素将会重叠显示。
        /// </summary>
        /// <param name="arrangeSize">分配给面板的实际大小。</param>
        /// <returns>返回实际使用的大小，通常为传入的 <paramref name="arrangeSize"/>。</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                // 如果 child 为 null 则跳过，否则将整个区域传递给子元素进行排列
                child?.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }
    }
}
