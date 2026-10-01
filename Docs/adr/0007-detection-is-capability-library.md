# 检测是能力库：接口隔离 YoloDotNet

Status: accepted

核心原则一句话：推理是一个独立的能力库（Framework.Detection，无 UI、无设备依赖），YOLO 五类任务是它先后落地的五个契约（检测 `IDetector`/`Detection`、姿态 `IPoseEstimator`/`PoseResult`、分割 `ISegmenter`/`SegmentationResult`、分类 `IClassifier`/`ClassificationResult`、旋转框 `IObbDetector`/`ObbResult`）；推理引擎 YoloDotNet 及其类型（SKBitmap、ObjectDetection、PoseEstimation、Segmentation、Classification、OBBDetection）不越过这条边界。

## 背景

技术路线定为：Python 侧训练 → ONNX 交付 → C# 侧加载推理（YoloDotNet 4.2.0 + CPU 执行提供方）。争议点在 C# 侧的形态：像配方/日志那样做成模块，还是只封装一个公共方法。

## 规则

1. **能力库而非模块**：Framework.Detection 不含页面、导航、存储，不引用 PrismUI/SemiControl；它比"配方/日志模块"薄——模块是"带界面的业务域"，检测只是"一项能力"，谁拿到 `ImageFrame` 谁就能用。
2. **接口先行**：调用方依赖 `IDetector`（`Detect(ImageFrame, confidence, iou)`），不依赖 YoloDotNet；换引擎（ONNX Runtime 直调、TensorRT、GPU 提供方）或换模型来源时，页面与宿主零改动。
3. **图像帧是通用输入**：Framework.Detection 引用 Framework.Imaging（net8.0 零依赖），以 `ImageFrame` 为输入——检测与显示消费同一种图像原语，谁也不依附于谁。
4. **模型路径来自配置**：`AppGlobals.YoloModelFilePath`（`Models\Yolo\best.onnx` 相对 AppDebug），模型文件不进仓库。
5. **实现不保证线程安全**：同一 `IDetector` 实例的 `Detect` 须串行调用；并发需求将来用实例池解决。
6. **引擎怪癖翻译到底**：与 ADR-0006 同理，引擎输出在适配器内归一化——letterbox 映射产生的负坐标/越界框由 YoloDetector 裁剪回图像范围、丢弃完全出界者，下游拿到的框永远在图像范围内。
7. **姿态估计同门同规**：姿态估计加入同一能力库（`IPoseEstimator`/`PoseResult`/`KeyPoint`），与检测共用 ImageFrame→SKBitmap 转换与全部边界规则；任务与模型必须匹配（pose 任务模型喂 `RunPoseEstimation`，喂给检测接口即抛类型转换异常——引擎按 ONNX 元数据自动路由）。
8. **导出布局必须匹配引擎模块**：YoloDotNet 按 ONNX 元数据的 `head` 路由模块（`Detect` → 通道优先老布局模块；`Pose26` → V26 模块），V26 姿态模块只认 **end2end 导出**（输出 `[1, 锚点数, 6+3×关键点]`，锚点优先、含类别通道）；老式通道优先布局（`[1, 56, 8400]`）会被把关键点坐标当类别号去索引标签数组，必抛越界异常。YOLO26 姿态模型导出须带 `nms=True`。踩坑实录见 2026-10-01 排查：`yolo26n-pose.onnx` 布局不符即崩，与能力库代码无关。
9. **分割同门同规，掩码怪癖消化在适配器**：实例分割加入同一能力库（`ISegmenter`/`SegmentationResult`/`SegmentMask`），共用 ImageFrame→SKBitmap 转换与框裁剪规则。掩码的引擎格式（按未裁剪外接框位打包：行优先、每字节最低位在前）不外泄——YoloSegmenter 把掩码裁剪平移到与结果框同尺寸同原点，`SegmentMask.IsForeground` 封装位运算，下游按图像坐标取样即可。与姿态不同，V26 分割模块兼容普通（非 end2end）导出，无需 `nms=True`。
10. **分类与旋转框补齐五任务**：分类（`IClassifier`/`ClassificationResult`）没有位置概念，整图 Top-K，无布局坑；旋转框（`IObbDetector`/`ObbResult`）沿用全部框裁剪规则，但引擎约定"矩形未旋转、角度绕矩形中心（弧度）"原样透进契约（`ObbResult.AngleRadians`），旋转后角点不裁剪——裁剪破坏四边形。**V26 旋转框模块与姿态同样只认 end2end 导出**（输出 `[1, 锚点数, 7]`：x,y,w,h,conf,label,angle），YOLO26 OBB 模型导出须带 `nms=True`。

## Considered Options

- **完整模块**（像配方/日志：Contracts + Infrastructure + UI 三件套）：弃用——检测目前没有持久化、没有配置管理，撑不起三件套；先有能力库，将来需要历史记录/版本管理时再升级。
- **公共方法**（Utils 里一个静态函数）：弃用——模型生命周期（加载慢、须复用、须释放）撑不起无状态函数；引擎类型会渗进调用方。
- **能力库 + 接口隔离（选定）**：依赖最小、边界清晰，测试页与将来产线检测共用同一入口。

## Consequences

- **许可注意**：YoloDotNet 本体 MIT；但 Ultralytics 工具链训练的模型受 AGPL-3.0 约束，闭源商用产品直接使用有合规风险——模型来源（自训框架、许可条款）是商业决策，换训练框架不影响本 ADR 的代码结构。
- 推理在 `Task.Run` 中执行（页面侧约定）；能力库自身不开线程。
- CPU 执行提供方先行；GPU（CUDA/TensorRT）是 YoloDotNet 独立包，按需加引用，接口不变。
- 骨架连线（`YoloPoseEstimator.CocoSkeletonEdges`）绑定 COCO 17 点拓扑——这是模型知识不是引擎知识，换自定义关键点数的姿态模型时须同步这份连线表；显示侧已按点数防护，点数不足时断线不崩。
