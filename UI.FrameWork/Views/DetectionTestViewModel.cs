using Framework.Core.Common;
using Framework.Detection;
using Framework.Imaging;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Navigation.Regions;
using PrismUI.Core;
using SemiControl.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：检测测试页 ViewModel——选一张图片跑 YOLO 推理，检测框叠加到 ImageViewer，
    //       验证 Framework.Detection 能力库。模型路径来自 AppGlobals，检测器随页面复用（模型只加载一次）。
    //       层级：主页→测试→检测测试（第 3 级，ADR-0005）。
    public class DetectionTestViewModel : BaseViewModel, INavigationAware
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private IDetector? _detector;
        private IPoseEstimator? _poseEstimator;
        private ISegmenter? _segmenter;
        private IClassifier? _classifier;
        private IObbDetector? _obbDetector;

        /// <summary>推理取图的原始帧：分割会往显示帧里叠色，原始帧另存，清除结果时还原。</summary>
        private ImageFrame? _sourceFrame;

        /// <summary>关键点显示阈值：低于该置信度的点不画、其连线断开。</summary>
        private const double KeyPointVisibleConfidence = 0.5;

        /// <summary>分类展示的候选数（Top-K）。</summary>
        private const int TopKClasses = 5;

        /// <summary>分割实例叠色调色板（BGR 序，与 BGR8 帧字节序一致），实例按序循环取色。</summary>
        private static readonly byte[][] SegmentPalette =
        {
            new byte[] { 0, 255, 0 },     // 绿
            new byte[] { 255, 255, 0 },   // 青
            new byte[] { 255, 0, 255 },   // 品红
            new byte[] { 0, 255, 255 },   // 黄
            new byte[] { 255, 165, 0 },   // 橙
        };

        public DetectionTestViewModel()
        {
            PickImageCommand = new DelegateCommand(PickImage, () => !IsInferring);
            RunCommand = new DelegateCommand(Run, () => Frame != null && !IsInferring);
            RunPoseCommand = new DelegateCommand(RunPose, () => Frame != null && !IsInferring);
            RunSegmentCommand = new DelegateCommand(RunSegment, () => Frame != null && !IsInferring);
            RunClassifyCommand = new DelegateCommand(RunClassify, () => Frame != null && !IsInferring);
            RunObbCommand = new DelegateCommand(RunObb, () => Frame != null && !IsInferring);
            ClearResultsCommand = new DelegateCommand(ClearResults, () => Results.Count > 0);

            Results.CollectionChanged += (_, _) => ClearResultsCommand.RaiseCanExecuteChanged();
        }

        #region 绑定属性

        private ImageFrame? _frame;
        /// <summary>当前显示帧。</summary>
        public ImageFrame? Frame
        {
            get => _frame;
            set
            {
                if (SetProperty(ref _frame, value))
                {
                    RunCommand.RaiseCanExecuteChanged();
                    RunPoseCommand.RaiseCanExecuteChanged();
                    RunSegmentCommand.RaiseCanExecuteChanged();
                    RunClassifyCommand.RaiseCanExecuteChanged();
                    RunObbCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>检测结果叠加（推理产出，图像像素坐标系）。</summary>
        public ObservableCollection<ViewerShape> Results { get; } = new();

        private bool _isInferring;
        /// <summary>是否正在推理（防重入：推理期间禁用选图与再运行）。</summary>
        public bool IsInferring
        {
            get => _isInferring;
            private set
            {
                if (SetProperty(ref _isInferring, value))
                {
                    PickImageCommand.RaiseCanExecuteChanged();
                    RunCommand.RaiseCanExecuteChanged();
                    RunPoseCommand.RaiseCanExecuteChanged();
                    RunSegmentCommand.RaiseCanExecuteChanged();
                    RunClassifyCommand.RaiseCanExecuteChanged();
                    RunObbCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _statusText = "未加载图片";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        #endregion

        #region 命令

        public DelegateCommand PickImageCommand { get; }
        public DelegateCommand RunCommand { get; }
        public DelegateCommand RunPoseCommand { get; }
        public DelegateCommand RunSegmentCommand { get; }
        public DelegateCommand RunClassifyCommand { get; }
        public DelegateCommand RunObbCommand { get; }
        public DelegateCommand ClearResultsCommand { get; }

        private void PickImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp",
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _sourceFrame = LoadImageFrame(dialog.FileName);
                Frame = _sourceFrame;
                Results.Clear();
                StatusText = $"已加载 {Path.GetFileName(dialog.FileName)}（{Frame.Width}×{Frame.Height}）";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：图片加载失败 {File}", dialog.FileName);
                StatusText = $"图片加载失败：{ex.Message}";
            }
        }

        private void Run()
        {
            if (Frame is null || IsInferring)
            {
                return;
            }

            if (_detector is null && !File.Exists(AppGlobals.YoloModelFilePath))
            {
                StatusText = $"未找到模型文件：{AppGlobals.YoloModelFilePath}";
                return;
            }

            // 永远推理原始帧：显示帧可能带着上次分割的叠色。
            ImageFrame frame = _sourceFrame!;
            IsInferring = true;
            StatusText = "推理中…";
            _ = RunAsync(frame);
        }

        private async Task RunAsync(ImageFrame frame)
        {
            try
            {
                (List<Detection> Detections, long ElapsedMs) result = await Task.Run(() =>
                {
                    _detector ??= new YoloDetector(AppGlobals.YoloModelFilePath);
                    var stopwatch = Stopwatch.StartNew();
                    IReadOnlyList<Detection> detections = _detector.Detect(frame);
                    stopwatch.Stop();
                    return (new List<Detection>(detections), stopwatch.ElapsedMilliseconds);
                });

                ShowDetections(result.Detections, result.ElapsedMs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：推理失败");
                StatusText = $"推理失败：{ex.Message}";
            }
            finally
            {
                IsInferring = false;
            }
        }

        private void ShowDetections(List<Detection> detections, long elapsedMs)
        {
            Results.Clear();
            foreach (Detection detection in detections)
            {
                ViewerShape rect = ViewerShape.Rectangle(detection.X, detection.Y, detection.Width, detection.Height);
                rect.Label = $"{detection.Label} {detection.Confidence:F2}";
                Results.Add(rect);
            }

            StatusText = $"推理完成：{detections.Count} 个目标，耗时 {elapsedMs} ms";
        }

        private void RunPose()
        {
            if (Frame is null || IsInferring)
            {
                return;
            }

            if (_poseEstimator is null && !File.Exists(AppGlobals.YoloPoseModelFilePath))
            {
                StatusText = $"未找到姿态模型文件：{AppGlobals.YoloPoseModelFilePath}";
                return;
            }

            // 永远推理原始帧：显示帧可能带着上次分割的叠色。
            ImageFrame frame = _sourceFrame!;
            IsInferring = true;
            StatusText = "姿态估计中…";
            _ = RunPoseAsync(frame);
        }

        private async Task RunPoseAsync(ImageFrame frame)
        {
            try
            {
                (List<PoseResult> Poses, long ElapsedMs) result = await Task.Run(() =>
                {
                    _poseEstimator ??= new YoloPoseEstimator(AppGlobals.YoloPoseModelFilePath);
                    var stopwatch = Stopwatch.StartNew();
                    IReadOnlyList<PoseResult> poses = _poseEstimator.Estimate(frame);
                    stopwatch.Stop();
                    return (new List<PoseResult>(poses), stopwatch.ElapsedMilliseconds);
                });

                ShowPoses(result.Poses, result.ElapsedMs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：姿态估计失败");
                StatusText = $"姿态估计失败：{ex.Message}";
            }
            finally
            {
                IsInferring = false;
            }
        }

        /// <summary>姿态结果可视化：骨架连线 + 关键点圆点 + 人体框。</summary>
        private void ShowPoses(List<PoseResult> poses, long elapsedMs)
        {
            Results.Clear();
            foreach (PoseResult pose in poses)
            {
                // 骨架连线：任一端点置信度不足则断开。
                foreach (int[] edge in YoloPoseEstimator.CocoSkeletonEdges)
                {
                    if (edge[0] >= pose.KeyPoints.Count || edge[1] >= pose.KeyPoints.Count)
                    {
                        continue;
                    }

                    KeyPoint start = pose.KeyPoints[edge[0]];
                    KeyPoint end = pose.KeyPoints[edge[1]];
                    if (start.Confidence < KeyPointVisibleConfidence || end.Confidence < KeyPointVisibleConfidence)
                    {
                        continue;
                    }

                    ViewerShape line = ViewerShape.Line(start.X, start.Y, end.X, end.Y);
                    Results.Add(line);
                }

                // 关键点圆点。
                foreach (KeyPoint keyPoint in pose.KeyPoints)
                {
                    if (keyPoint.Confidence < KeyPointVisibleConfidence)
                    {
                        continue;
                    }

                    ViewerShape dot = ViewerShape.Ellipse(keyPoint.X - 4, keyPoint.Y - 4, 8, 8);
                    Results.Add(dot);
                }

                // 人体框与置信度标签。
                ViewerShape rect = ViewerShape.Rectangle(pose.X, pose.Y, pose.Width, pose.Height);
                rect.Label = $"{pose.Label} {pose.Confidence:F2}";
                Results.Add(rect);
            }

            StatusText = $"姿态估计完成：{poses.Count} 人，耗时 {elapsedMs} ms";
        }

        private void RunSegment()
        {
            if (Frame is null || IsInferring)
            {
                return;
            }

            if (_segmenter is null && !File.Exists(AppGlobals.YoloSegmentModelFilePath))
            {
                StatusText = $"未找到分割模型文件：{AppGlobals.YoloSegmentModelFilePath}";
                return;
            }

            // 永远推理原始帧：显示帧可能带着上次分割的叠色。
            ImageFrame frame = _sourceFrame!;
            IsInferring = true;
            StatusText = "实例分割中…";
            _ = RunSegmentAsync(frame);
        }

        private async Task RunSegmentAsync(ImageFrame frame)
        {
            try
            {
                (List<SegmentationResult> Segments, long ElapsedMs) result = await Task.Run(() =>
                {
                    _segmenter ??= new YoloSegmenter(AppGlobals.YoloSegmentModelFilePath);
                    var stopwatch = Stopwatch.StartNew();
                    IReadOnlyList<SegmentationResult> segments = _segmenter.Segment(frame);
                    stopwatch.Stop();
                    return (new List<SegmentationResult>(segments), stopwatch.ElapsedMilliseconds);
                });

                ShowSegmentations(result.Segments, result.ElapsedMs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：实例分割失败");
                StatusText = $"实例分割失败：{ex.Message}";
            }
            finally
            {
                IsInferring = false;
            }
        }

        /// <summary>分割结果可视化：掩码叠色进显示帧（原始帧留在 _sourceFrame，清除时还原）+ 外接框标签。</summary>
        private void ShowSegmentations(List<SegmentationResult> segments, long elapsedMs)
        {
            Results.Clear();

            ImageFrame source = _sourceFrame!;
            var pixels = new byte[source.Data.Length];
            Array.Copy(source.Data, pixels, pixels.Length);

            for (int i = 0; i < segments.Count; i++)
            {
                SegmentationResult segment = segments[i];
                byte[] color = SegmentPalette[i % SegmentPalette.Length];

                // 叠色：掩码前景像素与实例色对半混合；掩码与框同原点，逐行扫描取样。
                int originX = (int)Math.Round(segment.X);
                int originY = (int)Math.Round(segment.Y);
                SegmentMask mask = segment.Mask;
                for (int my = 0; my < mask.Height; my++)
                {
                    int py = originY + my;
                    if (py < 0 || py >= source.Height)
                    {
                        continue;
                    }

                    int rowBase = (py * source.Width) + originX;
                    for (int mx = 0; mx < mask.Width; mx++)
                    {
                        if (!mask.IsForeground(mx, my))
                        {
                            continue;
                        }

                        int px = originX + mx;
                        if (px < 0 || px >= source.Width)
                        {
                            continue;
                        }

                        int offset = (rowBase + mx) * 3;
                        pixels[offset] = BlendChannel(pixels[offset], color[0]);
                        pixels[offset + 1] = BlendChannel(pixels[offset + 1], color[1]);
                        pixels[offset + 2] = BlendChannel(pixels[offset + 2], color[2]);
                    }
                }

                ViewerShape rect = ViewerShape.Rectangle(segment.X, segment.Y, segment.Width, segment.Height);
                rect.Label = $"{segment.Label} {segment.Confidence:F2}";
                Results.Add(rect);
            }

            Frame = new ImageFrame(source.Width, source.Height, source.PixelFormat, pixels);
            StatusText = $"实例分割完成：{segments.Count} 个实例，耗时 {elapsedMs} ms";
        }

        private void RunClassify()
        {
            if (Frame is null || IsInferring)
            {
                return;
            }

            if (_classifier is null && !File.Exists(AppGlobals.YoloClassifyModelFilePath))
            {
                StatusText = $"未找到分类模型文件：{AppGlobals.YoloClassifyModelFilePath}";
                return;
            }

            // 永远推理原始帧：显示帧可能带着上次分割的叠色。
            ImageFrame frame = _sourceFrame!;
            IsInferring = true;
            StatusText = "分类中…";
            _ = RunClassifyAsync(frame);
        }

        private async Task RunClassifyAsync(ImageFrame frame)
        {
            try
            {
                (List<ClassificationResult> Classes, long ElapsedMs) result = await Task.Run(() =>
                {
                    _classifier ??= new YoloClassifier(AppGlobals.YoloClassifyModelFilePath);
                    var stopwatch = Stopwatch.StartNew();
                    IReadOnlyList<ClassificationResult> classes = _classifier.Classify(frame, TopKClasses);
                    stopwatch.Stop();
                    return (new List<ClassificationResult>(classes), stopwatch.ElapsedMilliseconds);
                });

                ShowClassifications(result.Classes, result.ElapsedMs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：分类失败");
                StatusText = $"分类失败：{ex.Message}";
            }
            finally
            {
                IsInferring = false;
            }
        }

        /// <summary>分类可视化：分类没有位置概念，不叠形状，Top-K 候选进状态条。</summary>
        private void ShowClassifications(List<ClassificationResult> classes, long elapsedMs)
        {
            Results.Clear();
            string topK = string.Join("、", classes.Select(c => $"{c.Label} {c.Confidence:F2}"));
            StatusText = $"分类完成（耗时 {elapsedMs} ms）：{topK}";
        }

        private void RunObb()
        {
            if (Frame is null || IsInferring)
            {
                return;
            }

            if (_obbDetector is null && !File.Exists(AppGlobals.YoloObbModelFilePath))
            {
                StatusText = $"未找到旋转框模型文件：{AppGlobals.YoloObbModelFilePath}";
                return;
            }

            // 永远推理原始帧：显示帧可能带着上次分割的叠色。
            ImageFrame frame = _sourceFrame!;
            IsInferring = true;
            StatusText = "旋转框检测中…";
            _ = RunObbAsync(frame);
        }

        private async Task RunObbAsync(ImageFrame frame)
        {
            try
            {
                (List<ObbResult> Obbs, long ElapsedMs) result = await Task.Run(() =>
                {
                    _obbDetector ??= new YoloObbDetector(AppGlobals.YoloObbModelFilePath);
                    var stopwatch = Stopwatch.StartNew();
                    IReadOnlyList<ObbResult> obbs = _obbDetector.Detect(frame);
                    stopwatch.Stop();
                    return (new List<ObbResult>(obbs), stopwatch.ElapsedMilliseconds);
                });

                ShowObbs(result.Obbs, result.ElapsedMs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检测测试：旋转框检测失败");
                StatusText = $"旋转框检测失败：{ex.Message}";
            }
            finally
            {
                IsInferring = false;
            }
        }

        /// <summary>旋转框可视化：未旋转矩形绕中心转角后的四边形，画 4 条边 + 标签。
        /// 角点可能出画幅（目标部分在图外），不裁剪——裁剪破坏四边形。</summary>
        private void ShowObbs(List<ObbResult> obbs, long elapsedMs)
        {
            Results.Clear();
            foreach (ObbResult obb in obbs)
            {
                double centerX = obb.X + (obb.Width / 2);
                double centerY = obb.Y + (obb.Height / 2);
                double cos = Math.Cos(obb.AngleRadians);
                double sin = Math.Sin(obb.AngleRadians);

                (double X, double Y)[] corners =
                {
                    RotateCorner(obb.X, obb.Y, centerX, centerY, cos, sin),
                    RotateCorner(obb.X + obb.Width, obb.Y, centerX, centerY, cos, sin),
                    RotateCorner(obb.X + obb.Width, obb.Y + obb.Height, centerX, centerY, cos, sin),
                    RotateCorner(obb.X, obb.Y + obb.Height, centerX, centerY, cos, sin),
                };

                for (int i = 0; i < corners.Length; i++)
                {
                    (double X, double Y) start = corners[i];
                    (double X, double Y) end = corners[(i + 1) % corners.Length];
                    ViewerShape edge = ViewerShape.Line(start.X, start.Y, end.X, end.Y);

                    // 标签只挂第一条边，四条都挂会重复四遍。
                    if (i == 0)
                    {
                        edge.Label = $"{obb.Label} {obb.Confidence:F2}";
                    }

                    Results.Add(edge);
                }
            }

            StatusText = $"旋转框检测完成：{obbs.Count} 个目标，耗时 {elapsedMs} ms";
        }

        /// <summary>点绕中心旋转（与引擎绘制约定一致：SKCanvas.RotateRadians 同向）。</summary>
        private static (double X, double Y) RotateCorner(
            double px, double py, double centerX, double centerY, double cos, double sin)
        {
            double dx = px - centerX;
            double dy = py - centerY;
            return (centerX + (dx * cos) - (dy * sin), centerY + (dx * sin) + (dy * cos));
        }

        /// <summary>单通道对半混合。</summary>
        private static byte BlendChannel(byte original, byte overlay)
        {
            return (byte)((original + overlay) / 2);
        }

        /// <summary>清除结果：撤掉叠加形状，并把显示帧还原成原始帧（去掉分割叠色）。</summary>
        private void ClearResults()
        {
            Results.Clear();
            if (_sourceFrame is not null && !ReferenceEquals(Frame, _sourceFrame))
            {
                Frame = _sourceFrame;
            }
        }

        #endregion

        #region 图片加载

        /// <summary>
        /// 解码图片为 BGR8 图像帧：经 SkiaSharp 解码并统一转 Bgra8888，再压回 3 字节 BGR。
        /// </summary>
        private static ImageFrame LoadImageFrame(string path)
        {
            using SKBitmap? source = SKBitmap.Decode(path);
            if (source is null)
            {
                throw new InvalidOperationException("无法解码该图片文件。");
            }

            using SKBitmap converted = source.Copy(SKColorType.Bgra8888);
            int pixelCount = converted.Width * converted.Height;

            var bgra = new byte[pixelCount * 4];
            Marshal.Copy(converted.GetPixels(), bgra, 0, bgra.Length);

            var bgr = new byte[pixelCount * 3];
            for (int i = 0; i < pixelCount; i++)
            {
                // Bgra8888 内存序即 [B,G,R,A]（与 WPF Bgra32 一致，纯红实测 0,0,255,255），
                // 前 3 字节直取就是 BGR8，不做任何通道对调。
                bgr[i * 3] = bgra[i * 4];
                bgr[(i * 3) + 1] = bgra[(i * 4) + 1];
                bgr[(i * 3) + 2] = bgra[(i * 4) + 2];
            }

            return new ImageFrame(converted.Width, converted.Height, ImagePixelFormat.BGR8, bgr);
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 无需清理：无取流、无后台循环，检测器随页面复用。
        }

        #endregion
    }
}
