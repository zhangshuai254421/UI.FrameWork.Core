# 图像查看控件用 WriteableBitmap + WPF 矢量层渲染

Status: accepted

ImageViewer（显示图像帧的通用控件，见 CONTEXT.md）的渲染选型：帧本体用 BCL 的 WriteableBitmap 承载，`WritePixels` 一次整帧拷贝上屏；十字线、ROI 框选、标注叠加等矢量元素用 WPF 矢量层（DrawingVisual）画在帧之上。不引入 SkiaSharp / D3DImage。原因：目标是预览档（≥30fps @ 500 万像素），一次整帧拷贝绰绰有余；矢量交互直接复用 WPF 的命中测试、抗锯齿与主题系统；控件库保持零第三方 native 依赖。

## Considered Options

- **SkiaSharp 全权渲染**（帧 + 矢量都由 Skia 画）：弃用——给控件库带入 native 包依赖，而性能需求达不到需要它的量级；命中测试与主题联动都得自己重做。
- **D3DImage / InteropBitmap 共享内存**：弃用——零拷贝 GPU 路线复杂度高，预览档场景下相对 `WritePixels` 的收益不构成理由。
- **WriteableBitmap + WPF 矢量层（选定）**。

## Consequences

- 帧到显示格式的转换（Mono8/Mono16、RGB24/BGR24 → Bgra32）由控件在 `WritePixels` 前完成，写入内部复用缓冲，不在 UI 线程上反复分配。
- 若将来出现"检测级全分辨率逐帧显示"或"GPU 叠加"需求（信号：`WritePixels` 拷贝实测成为瓶颈），本决策需重新评估。
