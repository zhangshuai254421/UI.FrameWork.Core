// Controls/ImageViewer/ViewerShape.cs
using System;

namespace SemiControl.Controls
{
    /// <summary>图像上叠加图形的种类。标注与检测结果两层共用同一套形状模型。</summary>
    public enum ViewerShapeKind
    {
        /// <summary>矩形（轴对齐）。</summary>
        Rectangle,

        /// <summary>线段。</summary>
        Line,

        /// <summary>椭圆（由外接矩形定义；拖拽画圆即拖出外接框）。</summary>
        Ellipse,
    }

    /// <summary>
    /// 图像上的叠加图形，坐标一律为图像像素坐标，缩放平移时跟随图像。
    /// 用户标注只用轮廓（<see cref="Label"/> 恒空）；检测结果叠加可带文字标签。
    /// 位置可变（标注的拖动编辑就靠 <see cref="Move"/>），种类与归属创建后不变。
    /// </summary>
    public sealed class ViewerShape
    {
        private ViewerShape(ViewerShapeKind kind)
        {
            Kind = kind;
        }

        /// <summary>图形种类。</summary>
        public ViewerShapeKind Kind { get; }

        /// <summary>外接框左上角 X（矩形/椭圆；线段时为端点 1 的 X）。</summary>
        public double X { get; private set; }

        /// <summary>外接框左上角 Y（矩形/椭圆；线段时为端点 1 的 Y）。</summary>
        public double Y { get; private set; }

        /// <summary>外接框宽度（矩形/椭圆；线段恒为 0）。</summary>
        public double Width { get; private set; }

        /// <summary>外接框高度（矩形/椭圆；线段恒为 0）。</summary>
        public double Height { get; private set; }

        /// <summary>线段端点 2 的 X（仅 <see cref="ViewerShapeKind.Line"/> 使用）。</summary>
        public double LineX2 { get; private set; }

        /// <summary>线段端点 2 的 Y（仅 <see cref="ViewerShapeKind.Line"/> 使用）。</summary>
        public double LineY2 { get; private set; }

        /// <summary>文字标签。用户标注恒为 null；检测结果叠加用于显示检测输出的文字。</summary>
        public string? Label { get; set; }

        /// <summary>创建矩形。</summary>
        public static ViewerShape Rectangle(double x, double y, double width, double height)
        {
            var shape = new ViewerShape(ViewerShapeKind.Rectangle);
            shape.SetBounds(x, y, width, height);
            return shape;
        }

        /// <summary>创建椭圆（外接矩形由参数给定）。</summary>
        public static ViewerShape Ellipse(double x, double y, double width, double height)
        {
            var shape = new ViewerShape(ViewerShapeKind.Ellipse);
            shape.SetBounds(x, y, width, height);
            return shape;
        }

        /// <summary>创建线段。</summary>
        public static ViewerShape Line(double x1, double y1, double x2, double y2)
        {
            var shape = new ViewerShape(ViewerShapeKind.Line)
            {
                X = x1,
                Y = y1,
                LineX2 = x2,
                LineY2 = y2,
            };
            return shape;
        }

        /// <summary>整体平移 (dx, dy)（图像像素）。标注拖动编辑使用。</summary>
        public void Move(double dx, double dy)
        {
            X += dx;
            Y += dy;
            LineX2 += dx;
            LineY2 += dy;
        }

        /// <summary>
        /// 命中测试：判断图像坐标点 (x, y)（允许 <paramref name="tolerance"/> 图像像素容差）是否落在图形上。
        /// 矩形/椭圆按"点在内部或边上"计，线段按点到线段距离计。
        /// </summary>
        public bool HitTest(double x, double y, double tolerance)
        {
            switch (Kind)
            {
                case ViewerShapeKind.Rectangle:
                    return x >= X - tolerance && x <= X + Width + tolerance
                        && y >= Y - tolerance && y <= Y + Height + tolerance;

                case ViewerShapeKind.Ellipse:
                {
                    double rx = Width / 2 + tolerance;
                    double ry = Height / 2 + tolerance;
                    if (rx <= 0 || ry <= 0)
                    {
                        return false;
                    }

                    double cx = X + Width / 2;
                    double cy = Y + Height / 2;
                    double nx = (x - cx) / rx;
                    double ny = (y - cy) / ry;
                    return nx * nx + ny * ny <= 1;
                }

                case ViewerShapeKind.Line:
                    return DistanceToSegment(x, y, X, Y, LineX2, LineY2) <= tolerance;

                default:
                    return false;
            }
        }

        /// <summary>拖拽过程中的实时几何更新（仅控件内部使用）。宽高会被归一化为非负。</summary>
        internal void SetBounds(double x, double y, double width, double height)
        {
            if (width < 0)
            {
                x += width;
                width = -width;
            }

            if (height < 0)
            {
                y += height;
                height = -height;
            }

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>拖拽过程中的线段端点 2 实时更新（仅控件内部使用）。</summary>
        internal void SetLineEnd(double x2, double y2)
        {
            LineX2 = x2;
            LineY2 = y2;
        }

        /// <summary>点到线段的最短距离（端点处自然收敛为到端点的距离）。</summary>
        private static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            double abx = bx - ax;
            double aby = by - ay;
            double apx = px - ax;
            double apy = py - ay;
            double lengthSquared = abx * abx + aby * aby;

            double t = lengthSquared < double.Epsilon
                ? 0
                : Math.Clamp((apx * abx + apy * aby) / lengthSquared, 0.0, 1.0);

            double cx = ax + abx * t;
            double cy = ay + aby * t;
            double dx = px - cx;
            double dy = py - cy;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
