namespace Framework.Device
{
    /// <summary>
    /// 品牌无关的像素格式（相机图像的规范像素表示）。
    /// 拜耳值仅作规范表示存在：适配器须先去马赛克为 RGB 再产出相机帧，不得以拜耳格式出帧（Docs/adr/0006）。
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>未定义：适配器无法翻译的厂商格式（如 Mono10_Packed 打包格式），字节布局未知。</summary>
        Undefined = 0,

        /// <summary>8 位灰度，每像素 1 字节。</summary>
        Mono8 = 1,

        /// <summary>10 位灰度，每像素 2 字节（16 位小端容器，数据左对齐）。</summary>
        Mono10 = 2,

        /// <summary>12 位灰度，每像素 2 字节（16 位小端容器，数据左对齐）。</summary>
        Mono12 = 3,

        /// <summary>16 位灰度，每像素 2 字节。</summary>
        Mono16 = 4,

        /// <summary>8 位拜耳阵列（RGGB 排列），每像素 1 字节，需去马赛克还原真彩。</summary>
        BayerRG8 = 5,

        /// <summary>8 位拜耳阵列（BGGR 排列），每像素 1 字节。</summary>
        BayerBG8 = 6,

        /// <summary>8 位拜耳阵列（GRBG 排列），每像素 1 字节。</summary>
        BayerGR8 = 7,

        /// <summary>8 位拜耳阵列（GBRG 排列），每像素 1 字节。</summary>
        BayerGB8 = 8,

        /// <summary>24 位真彩，每像素 3 字节，R/G/B 顺序。</summary>
        RGB8 = 9,

        /// <summary>24 位真彩，每像素 3 字节，B/G/R 顺序。</summary>
        BGR8 = 10,
    }
}
