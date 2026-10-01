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
        /// <summary>测试用相机设备 ID。</summary>
        private const string DeviceId = "CameraA";

        /// <summary>设备管理器，按 ID 查找相机。</summary>
        private readonly DeviceManager _deviceManager;
        /// <summary>UI 线程调度器，用于把帧更新切回 UI 线程。</summary>
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        /// <summary>当前取流的相机；未取流时为空。</summary>
        private CameraBase? _camera;
        /// <summary>泵帧任务的取消源。</summary>
        private CancellationTokenSource? _streamCts;
        /// <summary>后台泵帧任务。</summary>
        private Task? _pumpTask;

        // 帧率统计：泵帧线程只做 Interlocked.Increment，定时器每秒读取并归零。
        private long _frameCounter;
        /// <summary>每秒读取帧计数并刷新 Fps 的 UI 定时器。</summary>
        private readonly DispatcherTimer _fpsTimer;

        /// <summary>构造：注入设备管理器，初始化各命令与帧率定时器。</summary>
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

        /// <summary>当前显示帧的后端字段。</summary>
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

        /// <summary>中心十字线开关的后端字段。</summary>
        private bool _isCenterCrosshair;
        /// <summary>中心十字线开关。</summary>
        public bool IsCenterCrosshair
        {
            get => _isCenterCrosshair;
            set => SetProperty(ref _isCenterCrosshair, value);
        }

        /// <summary>取流状态的后端字段。</summary>
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

        /// <summary>状态文本的后端字段。</summary>
        private string _statusText = "未开始";
        /// <summary>状态提示文本，绑定相机测试页的状态栏。</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>帧率的后端字段。</summary>
        private long _fps;
        /// <summary>每秒入帧数（控件侧收到的帧，非相机侧帧率）。</summary>
        public long Fps
        {
            get => _fps;
            set => SetProperty(ref _fps, value);
        }

        #endregion

        #region 命令

        /// <summary>开始取流命令：仅在未取流时可用，绑定相机测试页开始按钮。</summary>
        public DelegateCommand StartCommand { get; }
        /// <summary>停止取流命令：仅在取流中可用，绑定相机测试页停止按钮。</summary>
        public DelegateCommand StopCommand { get; }
        /// <summary>注入模拟检测结果命令：仅有当前帧时可用。</summary>
        public DelegateCommand InjectResultCommand { get; }
        /// <summary>清空检测结果命令：仅在有结果时可用。</summary>
        public DelegateCommand ClearResultsCommand { get; }

        /// <summary>开始取流：按 ID 找相机、切回调模式并启动泵帧与帧率统计；相机不存在时仅更新状态文本。</summary>
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

        /// <summary>停止取流：停帧率定时器、取消并等待泵帧退出，再停止相机采集并复位状态。</summary>
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

        /// <summary>泵帧循环：读取相机通道帧并转成图像帧，切回 UI 线程更新显示；取消即正常退出，异常仅记录。</summary>
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

        /// <summary>进入页面：无需初始化，空实现。</summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        /// <summary>始终复用当前实例作为导航目标。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>离开页面：立即停止取流，避免后台泵帧空转。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 离开页面即停止取流，避免后台泵帧空转。
            Stop();
        }

        #endregion
    }
}
