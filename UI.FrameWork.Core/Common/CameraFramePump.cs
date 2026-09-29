// Common/CameraFramePump.cs
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Framework.Device;
using SemiControl.Controls;

namespace UI.FrameWork.Core.Common
{
    /// <summary>
    /// 相机帧泵：把相机实时通道（<see cref="CameraBase.ChannelCameraData"/>）里的相机帧
    /// 转换成控件侧的图像帧，持续喂给一个 <see cref="ImageViewer"/>。相机帧到图像帧的映射
    /// 就住在这一层——显示控件不知道任何相机的存在（见 Docs/adr/0004）。
    ///
    /// 前提：相机已 StartAcquisition 且取流模式能让帧进入通道（Callback 持续推帧，或 Pull 由外部拉帧）。
    /// 本类只搬运，不负责取流启停。
    /// </summary>
    public sealed class CameraFramePump : IDisposable
    {
        private readonly CameraBase _camera;
        private readonly ImageViewer _viewer;
        private readonly CancellationTokenSource _cancellation = new();
        private Task? _pumpTask;
        private volatile bool _stopped;

        /// <summary>创建泵。<paramref name="viewer"/> 必须已创建（可在任意页面），帧经控件内部 marshal 上 UI 线程。</summary>
        public CameraFramePump(CameraBase camera, ImageViewer viewer)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
        }

        /// <summary>开始泵帧（重复调用无副作用）。通道为"容量 3 丢最旧"，显示永远追最新帧。</summary>
        public void Start()
        {
            if (_pumpTask is not null)
            {
                return;
            }

            _stopped = false;
            _pumpTask = Task.Run(PumpLoop);
        }

        /// <summary>停止泵帧并等待循环退出（不负责相机的 StopAcquisition，由调用方按生命周期管理）。</summary>
        public void Stop()
        {
            if (_pumpTask is null)
            {
                return;
            }

            _stopped = true;
            _cancellation.Cancel();
            try
            {
                _pumpTask.Wait();
            }
            catch (AggregateException)
            {
                // 循环内的取消异常已就地消化；这里兜底即可。
            }

            _pumpTask = null;
        }

        private async Task PumpLoop()
        {
            try
            {
                while (!_stopped)
                {
                    CameraData cameraData = await _camera.ChannelCameraData.Reader.ReadAsync(_cancellation.Token);
                    ImageFrame? frame = ToImageFrame(cameraData);
                    if (frame is not null)
                    {
                        _viewer.Show(frame);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stop 触发的正常退出路径。
            }
            catch (ChannelClosedException)
            {
                // 相机侧关闭通道（设备释放）同样视为正常退出。
            }
        }

        /// <summary>
        /// 相机帧 → 图像帧的格式映射：Mono8/16 与 RGB/BGR 直通；
        /// Mono10/12（16 位小端容器、数据左对齐）借用 Mono16 路径取高 8 位显示；
        /// Bayer 阵列与 Undefined 字节布局未知，v1 不支持显示，丢帧处理（落盘不受影响）。
        /// </summary>
        private static ImageFrame? ToImageFrame(CameraData cameraData)
        {
            ImagePixelFormat? format = cameraData.PixelFormat switch
            {
                PixelFormat.Mono8 => ImagePixelFormat.Mono8,
                PixelFormat.Mono16 => ImagePixelFormat.Mono16,
                PixelFormat.Mono10 or PixelFormat.Mono12 => ImagePixelFormat.Mono16,
                PixelFormat.RGB8 => ImagePixelFormat.RGB8,
                PixelFormat.BGR8 => ImagePixelFormat.BGR8,
                _ => null,
            };

            if (format is null || cameraData.Width <= 0 || cameraData.Height <= 0)
            {
                return null;
            }

            try
            {
                return new ImageFrame(cameraData.Width, cameraData.Height, format.Value, cameraData.ImageData);
            }
            catch (ArgumentException)
            {
                // 数据长度与格式不符：坏帧直接丢弃，不中断泵。
                return null;
            }
        }

        public void Dispose()
        {
            Stop();
            _cancellation.Dispose();
        }
    }
}
