// Controls/ImageViewer/ImagePixelFormat.cs
namespace SemiControl.Controls
{
    /// <summary>
    /// 图像查看控件支持的像素格式。与 Framework.Device 的 PixelFormat 保持映射关系而非共享类型
    /// （控件库零设备依赖，见 Docs/adr/0004）；设备侧的 Mono10/12 打包格式与 Bayer 阵列
    /// 由消费侧转换后再进入控件，本枚举不定义。
    /// </summary>
    public enum ImagePixelFormat
    {
        /// <summary>8 位灰度，每像素 1 字节。</summary>
        Mono8,

        /// <summary>16 位灰度，每像素 2 字节，小端容器、高字节为有效位（显示取高 8 位）。</summary>
        Mono16,

        /// <summary>24 位真彩，像素字节序 R-G-B。</summary>
        RGB8,

        /// <summary>24 位真彩，像素字节序 B-G-R。</summary>
        BGR8,
    }
}
