// Common/CameraFrameForwarder.cs
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Framework.Device.Domain;
using Framework.Imaging;
using SemiControl.Controls;
using UI.FrameWork.Core.Imaging;

namespace UI.FrameWork.Core.Common
{
    /// <summary>
    /// 相机帧转发器：把相机实时通道（<see cref="CameraBase.ChannelCameraData"/>）里的相机帧
    /// 转换成控件侧的图像帧，持续喂给一个 <see cref="ImageViewer"/>。相机帧到图像帧的映射
    /// 就住在这一层——显示控件不知道任何相机的存在（见 Docs/adr/0004）。
    ///
    /// 前提：相机已 StartAcquisition 且取流模式能让帧进入通道（Callback 持续推帧，或 Pull 由外部拉帧）。
    /// 本类只搬运，不负责取流启停。
    /// </summary>
    public sealed class CameraFrameForwarder : IDisposable
    {
        /// <summary>帧来源相机。</summary>
        private readonly CameraBase _camera;
        /// <summary>目标显示控件。</summary>
        private readonly ImageViewer _viewer;
        /// <summary>停止转发用的取消源。</summary>
        private readonly CancellationTokenSource _cancellation = new();
        /// <summary>后台转发任务；为空表示尚未启动。</summary>
        private Task? _forwardTask;
        /// <summary>停止标志，配合取消令牌让转发循环尽快退出。</summary>
        private volatile bool _stopped;

        /// <summary>创建转发器。<paramref name="viewer"/> 必须已创建（可在任意页面），帧经控件内部 marshal 上 UI 线程。</summary>
        public CameraFrameForwarder(CameraBase camera, ImageViewer viewer)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
        }

        /// <summary>开始转发（重复调用无副作用）。通道为"容量 3 丢最旧"，显示永远追最新帧。</summary>
        public void Start()
        {
            if (_forwardTask is not null)
            {
                return;
            }

            _stopped = false;
            _forwardTask = Task.Run(ForwardLoop);
        }

        /// <summary>停止转发并等待循环退出（不负责相机的 StopAcquisition，由调用方按生命周期管理）。</summary>
        public void Stop()
        {
            if (_forwardTask is null)
            {
                return;
            }

            _stopped = true;
            _cancellation.Cancel();
            try
            {
                _forwardTask.Wait();
            }
            catch (AggregateException)
            {
                // 循环内的取消异常已就地消化；这里兜底即可。
            }

            _forwardTask = null;
        }

        /// <summary>转发循环：持续读取相机帧并转换为图像帧送显；取消或通道关闭即正常退出，不抛异常。</summary>
        private async Task ForwardLoop()
        {
            try
            {
                while (!_stopped)
                {
                    CameraData cameraData = await _camera.ChannelCameraData.Reader.ReadAsync(_cancellation.Token);
                    ImageFrame? frame = cameraData.ToImageFrame();
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

        /// <summary>停止转发并释放取消源；重复释放无副作用。</summary>
        public void Dispose()
        {
            Stop();
            _cancellation.Dispose();
        }
    }
}
