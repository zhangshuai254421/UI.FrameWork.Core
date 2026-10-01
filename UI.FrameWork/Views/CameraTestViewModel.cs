using Framework.Device.Domain;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Navigation.Regions;
using PrismUI.Core;
using SemiControl.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using Framework.Imaging;
using UI.FrameWork.Core.Imaging;

namespace SemiAppliaction.Views
{
    // 作者：Zhang Shuai
    // 描述：相机测试页 ViewModel——真实相机连续取流喂给 ImageViewer，验证控件的缩放、平移、
    //       ROI、标注与检测结果叠加在实时帧下的表现。层级：主页→测试→相机测试（第 3 级，ADR-0005）。
    public class CameraTestViewModel : BaseViewModel, INavigationAware
    {
        private const string DeviceId = "CameraA";

        private readonly DeviceManager _deviceManager;
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private CameraBase? _camera;
        private CancellationTokenSource? _streamCts;
        private Task? _pumpTask;

        // 帧率统计：泵帧线程只做 Interlocked.Increment，定时器每秒读取并归零。
        private long _frameCounter;
        private readonly DispatcherTimer _fpsTimer;

        public CameraTestViewModel(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;

            StartCommand = new DelegateCommand(Start, () => !IsStreaming);
            StopCommand = new DelegateCommand(Stop, () => IsStreaming);
            InjectResultCommand = new DelegateCommand(InjectResult, () => Frame != null);
            ClearResultsCommand = new DelegateCommand(() => Results.Clear(), () => Results.Count > 0);

            // 检测结果集合增删时刷新相关按钮可用态。
            Results.CollectionChanged += (_, _) =>
            {
                InjectResultCommand.RaiseCanExecuteChanged();
                ClearResultsCommand.RaiseCanExecuteChanged();
            };

            _fpsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _fpsTimer.Tick += (_, _) => Fps = Interlocked.Exchange(ref _frameCounter, 0);
        }

        #region 绑定属性

        private ImageFrame? _frame;
        /// <summary>当前显示帧。ImageFrame 不可变，控件转换进内部缓冲后不再持有原数组（ADR-0004）。</summary>
        public ImageFrame? Frame
        {
            get => _frame;
            set
            {
                if (SetProperty(ref _frame, value))
                {
                    InjectResultCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>检测结果叠加（宿主给定、只读显示）；模拟检测结果从这里注入。</summary>
        public ObservableCollection<ViewerShape> Results { get; } = new();

        private bool _isCenterCrosshair;
        /// <summary>中心十字线开关。</summary>
        public bool IsCenterCrosshair
        {
            get => _isCenterCrosshair;
            set => SetProperty(ref _isCenterCrosshair, value);
        }

        private bool _isStreaming;
        /// <summary>是否正在取流。</summary>
        public bool IsStreaming
        {
            get => _isStreaming;
            private set
            {
                if (SetProperty(ref _isStreaming, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                    StopCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _statusText = "未开始";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private long _fps;
        /// <summary>每秒入帧数（控件侧收到的帧，非相机侧帧率）。</summary>
        public long Fps
        {
            get => _fps;
            set => SetProperty(ref _fps, value);
        }

        #endregion

        #region 命令

        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand InjectResultCommand { get; }
        public DelegateCommand ClearResultsCommand { get; }

        private void Start()
        {
            if (IsStreaming)
            {
                return;
            }

            IDevice? device = _deviceManager.GetDevice(DeviceId);
            if (device is not CameraBase camera)
            {
                Logger.LogWarning("相机测试：设备 {DeviceId} 不存在或不是相机", DeviceId);
                StatusText = $"未找到相机 {DeviceId}";
                return;
            }

            _camera = camera;
            // Callback 模式：适配器持续把帧推入 ChannelCameraData（容量 3、丢最旧帧，保实时）。
            camera.GrabMode = CameraGrabMode.Callback;
            camera.StartAcquisition();

            _streamCts = new CancellationTokenSource();
            _pumpTask = PumpFramesAsync(camera.ChannelCameraData, _streamCts.Token);

            Fps = 0;
            IsStreaming = true;
            StatusText = "取流中";
            _fpsTimer.Start();
        }

        private void Stop()
        {
            if (!IsStreaming)
            {
                return;
            }

            _fpsTimer.Stop();
            _streamCts?.Cancel();
            try
            {
                _pumpTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // 泵帧循环内部已记录异常，这里只等它退出。
            }
            _streamCts?.Dispose();
            _streamCts = null;
            _pumpTask = null;

            _camera?.StopAcquisition();
            _camera = null;

            Fps = 0;
            IsStreaming = false;
            StatusText = "已停止";
        }

        /// <summary>模拟检测结果：在当前帧上叠加矩形/线段/椭圆各一，验证检测结果叠加能力。</summary>
        private void InjectResult()
        {
            if (Frame is null)
            {
                return;
            }

            double w = Frame.Width;
            double h = Frame.Height;
            ViewerShape rect = ViewerShape.Rectangle(w * 0.25, h * 0.25, w * 0.5, h * 0.5);
            ViewerShape line = ViewerShape.Line(w * 0.25, h * 0.75, w * 0.75, h * 0.75);
            ViewerShape ellipse = ViewerShape.Ellipse(w * 0.4, h * 0.1, w * 0.2, h * 0.15);
            rect.Label = "OK 0.98";
            line.Label = "基准线";
            ellipse.Label = "0.87";
            Results.Clear();
            Results.Add(rect);
            Results.Add(line);
            Results.Add(ellipse);
            StatusText = "已叠加模拟检测结果";
        }

        #endregion

        #region 泵帧

        private async Task PumpFramesAsync(Channel<CameraData> channel, CancellationToken ct)
        {
            try
            {
                await foreach (CameraData data in channel.Reader.ReadAllAsync(ct))
                {
                    ImageFrame? frame = data.ToImageFrame(Logger);
                    if (frame is null)
                    {
                        continue;
                    }

                    // 绑定属性必须在 UI 线程更新；数据数组无需拷贝（ImageFrame 契约见 ADR-0004）。
                    await _dispatcher.InvokeAsync(() => Frame = frame);
                    Interlocked.Increment(ref _frameCounter);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止取流。
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "相机测试：泵帧循环异常退出");
                _dispatcher.Invoke(() => StatusText = "泵帧异常，已停止");
            }
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
            // 离开页面即停止取流，避免后台泵帧空转。
            Stop();
        }

        #endregion
    }
}
