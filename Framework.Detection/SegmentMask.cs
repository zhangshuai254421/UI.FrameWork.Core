using System;

namespace Framework.Detection
{
    /// <summary>
    /// 分割掩码：与结果外接矩形同尺寸的前景/背景位图。
    /// 位打包布局（引擎约定，已反编译核实）：像素按行优先排列，每字节从最低位到最高位
    /// 依次对应 8 个像素（LSB-first），字节数为 ⌈宽×高÷8⌉。调用方一般不直接碰位运算，
    /// 用 <see cref="IsForeground"/> 取样即可。
    /// </summary>
    public sealed record SegmentMask(int Width, int Height, byte[] BitPackedData)
    {
        /// <summary>
        /// 判断掩码内坐标 (x, y) 是否为前景像素（目标本体）。
        /// 坐标相对掩码左上角（即结果外接矩形的左上角），越界一律返回 false。
        /// </summary>
        public bool IsForeground(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return false;
            }

            int index = (y * Width) + x;
            return (BitPackedData[index >> 3] & (1 << (index & 7))) != 0;
        }
    }
}
