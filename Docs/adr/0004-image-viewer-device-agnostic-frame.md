# 图像查看控件零设备依赖，自带 ImageFrame 抽象

Status: accepted

ImageViewer 放在 SemiControl_Net 控件库，定义自己的帧类型 ImageFrame（像素数据 + 宽高 + 像素格式），完全不引用 Framework.Device、不知道 CameraData 的存在；相机帧到图像帧的映射由消费侧（UI.FrameWork.Core）的泵完成——读 `Channel<CameraData>`、包成 ImageFrame、赋给控件的 `Frame` 依赖属性。原因：Framework.Device 目标框架是 net8.0（非 Windows），WPF 控件物理上放不进去；控件库保持零设备依赖才能被任意宿主复用（Demo 工程用合成帧即可演示，无需硬件）；而映射层薄到只是三行循环，不值得为省它污染依赖。

## Considered Options

- **控件直接吃 CameraData / Channel\<CameraData\>**：弃用——依赖方向倒置，且设备库 TFM 需改为 windows 才能被 WPF 控件引用，破坏分层。
- **控件库自定义 ImageFrame，消费侧泵适配（选定）**。

## Consequences

- ImageFrame 的像素格式枚举是控件库自己的定义，与 Framework.Device 的 PixelFormat 保持**映射关系而非共享类型**；设备侧新增格式时两边各自登记映射。
- 相机通道"最新帧优先"（容量 3、丢最旧）的语义由泵与控件的帧合并共同延续；控件只见"有人给我 Frame"，不知道通道概念。
