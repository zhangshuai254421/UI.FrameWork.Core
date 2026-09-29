// ViewerShapeTests.cs
using SemiControl.Controls;
using Xunit;

namespace SemiControl_Net.Tests
{
    /// <summary>叠加形状契约：命中测试几何与平移不变性。标注与检测结果两层共用这套模型。</summary>
    public class ViewerShapeTests
    {
        // ---------- 矩形 ----------

        [Fact]
        public void Rectangle_HitInsideAndOnBorder()
        {
            var rect = ViewerShape.Rectangle(10, 10, 30, 20);

            Assert.True(rect.HitTest(25, 20, 0));   // 内部
            Assert.True(rect.HitTest(10, 10, 0));   // 左上角点
            Assert.True(rect.HitTest(40, 30, 0));   // 右下角点
            Assert.False(rect.HitTest(9, 15, 0));   // 外部
            Assert.True(rect.HitTest(9, 15, 2));    // 容差救回
        }

        [Fact]
        public void Rectangle_Move_ShiftsAllCoordinates()
        {
            var rect = ViewerShape.Rectangle(10, 10, 30, 20);
            rect.Move(5, -3);

            Assert.Equal(15, rect.X);
            Assert.Equal(7, rect.Y);
            Assert.Equal(30, rect.Width);
            Assert.True(rect.HitTest(20, 12, 0));
            Assert.False(rect.HitTest(12, 12, 0));
        }

        // ---------- 椭圆 ----------

        [Fact]
        public void Ellipse_HitCenterInside_OutsideCornerMiss()
        {
            // 外接框 (0,0,40,20)：中心 (20,10)，半轴 (20,10)
            var ellipse = ViewerShape.Ellipse(0, 0, 40, 20);

            Assert.True(ellipse.HitTest(20, 10, 0));                          // 中心
            Assert.True(ellipse.HitTest(38, 10, 0));                          // 长轴端点附近
            Assert.False(ellipse.HitTest(39, 19, 0));                         // 外接框角（椭圆外）
            Assert.False(ellipse.HitTest(41, 10, 0));                         // 长轴外 1 像素
            Assert.True(ellipse.HitTest(41, 10, 2));                          // 容差救回
        }

        // ---------- 线段 ----------

        [Fact]
        public void Line_HitsNearMidpoint_MissesBeyondTolerance()
        {
            var line = ViewerShape.Line(0, 0, 40, 0);

            Assert.True(line.HitTest(20, 1, 2));   // 中点附近，容差内
            Assert.False(line.HitTest(20, 5, 2));  // 垂直距离 5 > 容差
        }

        [Fact]
        public void Line_DistanceConvergesToEndpoint()
        {
            var line = ViewerShape.Line(10, 10, 20, 10);

            // 垂足落在线段延长线外 → 收敛为到端点 (10,10) 的距离
            Assert.True(line.HitTest(5, 10, 5));
            Assert.False(line.HitTest(4, 10, 5));
        }

        [Fact]
        public void Line_Move_ShiftsBothEnds()
        {
            var line = ViewerShape.Line(0, 0, 40, 0);
            line.Move(10, 10);

            Assert.Equal(10, line.X);
            Assert.Equal(10, line.Y);
            Assert.Equal(50, line.LineX2);
            Assert.Equal(10, line.LineY2);
            Assert.True(line.HitTest(30, 10, 0));
        }

        // ---------- 标签 ----------

        [Fact]
        public void Label_DefaultsNull_AndUserAnnotationKeepsItNull()
        {
            // 用户标注只有轮廓不带文字：控件画标注时 Label 恒空，契约在此固化。
            var shape = ViewerShape.Rectangle(0, 0, 5, 5);
            Assert.Null(shape.Label);

            shape.Label = "缺陷A";
            Assert.Equal("缺陷A", shape.Label);
        }

        // ---------- 拖拽几何（控件内部使用） ----------

        [Fact]
        public void RectangleFactory_NormalizesNegativeWidthHeight()
        {
            // 从右下往左上拖：起点 (50,40)，当前 (20,10) → 归一化为 (20,10,30,30)。
            // 控件拖拽正是把负宽高直接交给工厂/边界更新来归一化。
            var shape = ViewerShape.Rectangle(50, 40, -30, -30);

            Assert.Equal(20, shape.X);
            Assert.Equal(10, shape.Y);
            Assert.Equal(30, shape.Width);
            Assert.Equal(30, shape.Height);
        }
    }
}
