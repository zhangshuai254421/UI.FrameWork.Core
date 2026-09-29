// Controls/ImageViewer/ImageViewer.cs
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SemiControl.Controls
{
    /// <summary>
    /// 图像查看控件：显示 <see cref="ImageFrame"/>，提供滚轮缩放（以光标为锚）、
    /// 适应窗口/1:1、ROI 工作矩形、中心十字线、用户标注（右键菜单画矩形/直线/圆形，可选中拖动、
    /// Delete 删除）与检测结果叠加（宿主给定的轮廓+文字，只读）。
    /// 左键是"智能"按键，无需工具模式：按在框（ROI/标注）上即拖动/缩放该框，
    /// 按在空白处即平移画面；新建 ROI 与标注统一走右键菜单、选定后下一笔落框。
    /// 自带底部信息条（坐标 + 像素值 + 缩放百分比），不带工具条——宿主经公开方法自行组按钮。
    /// 控件零设备依赖：只认图像帧，不知道任何相机的存在（见 Docs/adr/0004）。
    /// </summary>
    [TemplatePart(Name = "PART_Presenter", Type = typeof(ImageViewerPresenter))]
    [TemplatePart(Name = "PART_InfoBar", Type = typeof(Border))]
    [TemplatePart(Name = "PART_InfoPosition", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_InfoPixel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_InfoZoom", Type = typeof(TextBlock))]
    public class ImageViewer : Control
    {
        // ============ 依赖属性 ============

        public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
            "Frame", typeof(ImageFrame), typeof(ImageViewer),
            new PropertyMetadata(null, OnFrameChanged));

        public static readonly DependencyProperty IsCenterCrosshairProperty = DependencyProperty.Register(
            "IsCenterCrosshair", typeof(bool), typeof(ImageViewer),
            new PropertyMetadata(false));

        public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
            "MinZoom", typeof(double), typeof(ImageViewer),
            new PropertyMetadata(0.1, OnZoomRangeChanged));

        public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
            "MaxZoom", typeof(double), typeof(ImageViewer),
            new PropertyMetadata(16.0, OnZoomRangeChanged));

        public static readonly DependencyProperty RoiRectProperty = DependencyProperty.Register(
            "RoiRect", typeof(Int32Rect), typeof(ImageViewer),
            new PropertyMetadata(Int32Rect.Empty, OnRoiRectChanged));

        public static readonly DependencyProperty AnnotationsProperty = DependencyProperty.Register(
            "Annotations", typeof(ObservableCollection<ViewerShape>), typeof(ImageViewer),
            new PropertyMetadata(null, OnShapeCollectionChanged));

        public static readonly DependencyProperty ResultsProperty = DependencyProperty.Register(
            "Results", typeof(ObservableCollection<ViewerShape>), typeof(ImageViewer),
            new PropertyMetadata(null, OnShapeCollectionChanged));

        public static readonly DependencyProperty AnnotationBrushProperty = DependencyProperty.Register(
            "AnnotationBrush", typeof(Brush), typeof(ImageViewer),
            new PropertyMetadata(CreateFrozenBrush(0x2F, 0x88, 0xFF)));

        public static readonly DependencyProperty ResultBrushProperty = DependencyProperty.Register(
            "ResultBrush", typeof(Brush), typeof(ImageViewer),
            new PropertyMetadata(CreateFrozenBrush(0xFF, 0x8C, 0x00)));

        public static readonly DependencyProperty RoiBrushProperty = DependencyProperty.Register(
            "RoiBrush", typeof(Brush), typeof(ImageViewer),
            new PropertyMetadata(CreateFrozenBrush(0xFF, 0xD7, 0x00)));

        public static readonly DependencyProperty CenterCrosshairBrushProperty = DependencyProperty.Register(
            "CenterCrosshairBrush", typeof(Brush), typeof(ImageViewer),
            new PropertyMetadata(CreateFrozenBrush(0x12, 0x8A, 0x3B)));

        // 只读输出：当前缩放百分比（100 = 1:1）
        private static readonly DependencyPropertyKey CurrentZoomPropertyKey = DependencyProperty.RegisterReadOnly(
            "CurrentZoom", typeof(double), typeof(ImageViewer), new PropertyMetadata(100.0));

        public static readonly DependencyProperty CurrentZoomProperty = CurrentZoomPropertyKey.DependencyProperty;

        // 只读输出：鼠标处的图像像素坐标
        private static readonly DependencyPropertyKey MouseImagePositionPropertyKey = DependencyProperty.RegisterReadOnly(
            "MouseImagePosition", typeof(Point), typeof(ImageViewer), new PropertyMetadata(new Point(-1, -1)));

        public static readonly DependencyProperty MouseImagePositionProperty = MouseImagePositionPropertyKey.DependencyProperty;

        // 只读输出：鼠标处的像素值文本（灰度或 R/G/B）
        private static readonly DependencyPropertyKey PixelValueTextPropertyKey = DependencyProperty.RegisterReadOnly(
            "PixelValueText", typeof(string), typeof(ImageViewer), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PixelValueTextProperty = PixelValueTextPropertyKey.DependencyProperty;

        /// <summary>ROI 工作矩形变化（拖拽过程实时触发，包含拖动/缩放/清除）。</summary>
        public event EventHandler<Int32Rect>? RoiChanged;

        private readonly Dispatcher _dispatcher;
        private ImageViewerViewport _viewport = new();
        private ImageViewerPresenter? _presenter;
        private TextBlock? _infoPosition;
        private TextBlock? _infoPixel;
        private TextBlock? _infoZoom;

        // ============ 帧渲染管线（合并为最新帧） ============
        private ImageFrame? _pendingFrame;
        private ImageFrame? _currentFrame;
        private WriteableBitmap? _bitmap;
        private byte[]? _convertBuffer;
        private int _renderQueued;
        private bool _fittedOnce;
        private bool _autoFit = true; // 用户手动缩放/平移后置 false，窗口尺寸变化时不再自动重铺

        // ============ 鼠标交互状态机 ============
        private enum DragMode
        {
            None,
            Panning,
            DrawingRoi,
            DrawingShape, // 右键菜单选定形状后的下一笔标注
            MovingRoi,
            ResizingRoi,
            MovingShape,
        }

        private DragMode _dragMode = DragMode.None;
        private Point _dragStartScreen;      // 拖拽起点（屏幕/控件坐标）
        private Point _dragStartImage;       // 拖拽起点（图像坐标）
        private Point? _lastMouseScreen;     // 最近一次鼠标位置（帧刷新时重算像素读数用）
        private Int32Rect _roiDragStartRect; // ROI 拖动/缩放前的矩形
        private int _resizeCorner;           // ROI 缩放时被拖的角（0..3）
        private ViewerShapeKind? _pendingAnnotationKind; // 右键菜单选定、等待落笔的标注形状
        private bool _pendingRoiDraw;      // 右键菜单选定、等待落笔画 ROI 新框
        private ViewerShape? _draftShape;    // 正在拖拽绘制中的形状
        private ViewerShape? _selectedShape; // 当前选中的标注

        public ImageViewer()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Focusable = true;
            Annotations = new ObservableCollection<ViewerShape>();
            Results = new ObservableCollection<ViewerShape>();
            ContextMenu = BuildContextMenu();
        }

        // ============ 属性包装 ============

        /// <summary>当前显示的图像帧。控件内部把连发多帧合并为最新帧，按自己的节奏渲染。</summary>
        public ImageFrame? Frame
        {
            get => (ImageFrame?)GetValue(FrameProperty);
            set => SetValue(FrameProperty, value);
        }

        /// <summary>是否显示中心十字线（十字钉在图像中心，跟随缩放平移）。</summary>
        public bool IsCenterCrosshair
        {
            get => (bool)GetValue(IsCenterCrosshairProperty);
            set => SetValue(IsCenterCrosshairProperty, value);
        }

        /// <summary>允许的最小缩放倍率，默认 0.1。</summary>
        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        /// <summary>允许的最大缩放倍率，默认 16。</summary>
        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        /// <summary>ROI 工作矩形（图像像素坐标）。Empty 表示无 ROI。</summary>
        public Int32Rect RoiRect
        {
            get => (Int32Rect)GetValue(RoiRectProperty);
            set => SetValue(RoiRectProperty, value);
        }

        /// <summary>用户标注集合（图像像素坐标，跨帧保留）。可整体替换；绘制/拖动/删除由控件维护。</summary>
        public ObservableCollection<ViewerShape> Annotations
        {
            get => (ObservableCollection<ViewerShape>)GetValue(AnnotationsProperty)!;
            set => SetValue(AnnotationsProperty, value);
        }

        /// <summary>检测结果叠加集合（轮廓+文字，宿主随检测周期整体替换，控件只读显示）。</summary>
        public ObservableCollection<ViewerShape> Results
        {
            get => (ObservableCollection<ViewerShape>)GetValue(ResultsProperty)!;
            set => SetValue(ResultsProperty, value);
        }

        /// <summary>标注轮廓画笔（样式默认取主题 PrimaryBrush）。</summary>
        public Brush AnnotationBrush
        {
            get => (Brush)GetValue(AnnotationBrushProperty);
            set => SetValue(AnnotationBrushProperty, value);
        }

        /// <summary>检测结果画笔与标签底色（样式默认取主题 WarningBrush）。</summary>
        public Brush ResultBrush
        {
            get => (Brush)GetValue(ResultBrushProperty);
            set => SetValue(ResultBrushProperty, value);
        }

        /// <summary>ROI 工作矩形画笔。</summary>
        public Brush RoiBrush
        {
            get => (Brush)GetValue(RoiBrushProperty);
            set => SetValue(RoiBrushProperty, value);
        }

        /// <summary>中心十字线画笔（样式默认取主题 NormalBrush 绿色）。</summary>
        public Brush CenterCrosshairBrush
        {
            get => (Brush)GetValue(CenterCrosshairBrushProperty);
            set => SetValue(CenterCrosshairBrushProperty, value);
        }

        /// <summary>当前缩放百分比（只读，100 = 1:1）。</summary>
        public double CurrentZoom => (double)GetValue(CurrentZoomProperty);

        /// <summary>鼠标处的图像像素坐标（只读，鼠标离开画面后为 (-1,-1)）。</summary>
        public Point MouseImagePosition => (Point)GetValue(MouseImagePositionProperty);

        /// <summary>鼠标处的像素值文本（只读）。</summary>
        public string PixelValueText => (string)GetValue(PixelValueTextProperty);

        /// <summary>内部视口（供渲染表面取缩放/偏移）。</summary>
        internal ImageViewerViewport Viewport => _viewport;

        // ============ 公开方法 ============

        /// <summary>
        /// 显示一帧图像。可跨线程调用（泵在采集线程上直接调它即可，无需自己写 Dispatcher）。
        /// <see cref="Frame"/> 属性本身受 WPF 线程亲和限制，跨线程赋帧请一律走本方法；XAML 绑定天然在 UI 线程，不受影响。
        /// </summary>
        public void Show(ImageFrame frame)
        {
            if (frame is null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(() => Frame = frame);
                return;
            }

            Frame = frame;
        }

        /// <summary>适应窗口：整幅图像可见并居中。</summary>
        public void FitToWindow() => ApplyView(viewport => viewport.FitTo(ImageWidth, ImageHeight, ViewW, ViewH));

        /// <summary>实际大小：缩放复位 1:1 并居中。</summary>
        public void ActualSize() => ApplyView(viewport => viewport.ActualSize(ImageWidth, ImageHeight, ViewW, ViewH));

        /// <summary>清除全部用户标注。</summary>
        public void ClearAnnotations() => Annotations.Clear();

        /// <summary>清除 ROI 工作矩形。</summary>
        public void ClearRoi() => RoiRect = Int32Rect.Empty;

        // ============ 模板与生命周期 ============

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_presenter is not null)
            {
                _presenter.SizeChanged -= Presenter_SizeChanged;
            }

            _presenter = GetTemplateChild("PART_Presenter") as ImageViewerPresenter;
            _infoPosition = GetTemplateChild("PART_InfoPosition") as TextBlock;
            _infoPixel = GetTemplateChild("PART_InfoPixel") as TextBlock;
            _infoZoom = GetTemplateChild("PART_InfoZoom") as TextBlock;

            if (_presenter is not null)
            {
                _presenter.Owner = this;
                _presenter.Bitmap = _bitmap; // 帧可能先于模板应用到达，这里补挂位图
                _presenter.SizeChanged += Presenter_SizeChanged;
            }

            UpdateInfoBar();
        }

        private void Presenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 用户尚未手动操作视图时（含首帧自动适应），窗口尺寸变化保持"适应窗口"。
            if (_autoFit)
            {
                FitToWindow();
            }
        }

        private static void OnFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((ImageViewer)d).EnqueueRender((ImageFrame?)e.NewValue);

        private static void OnZoomRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageViewer)d;
            // SetScaleRange 防御式校正非法区间，不抛异常。
            control._viewport.SetScaleRange(control.MinZoom, control.MaxZoom);
        }

        private static void OnRoiRectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageViewer)d;
            control.RoiChanged?.Invoke(control, (Int32Rect)e.NewValue);
            control.InvalidatePresenter();
        }

        private static void OnShapeCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageViewer)d;

            if (e.OldValue is ObservableCollection<ViewerShape> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnShapeCollectionItemChanged;
            }

            if (e.NewValue is ObservableCollection<ViewerShape> newCollection)
            {
                newCollection.CollectionChanged += control.OnShapeCollectionItemChanged;
            }

            control.InvalidatePresenter();
        }

        private void OnShapeCollectionItemChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => InvalidatePresenter(); // 集合内容变化即重绘；形状自身位移由控件在拖拽中自行触发重绘

        // ============ 帧渲染管线 ============

        /// <summary>入队渲染：可从任意线程调用；连发多帧只渲染最新（丢帧内聚在控件里）。</summary>
        private void EnqueueRender(ImageFrame? frame)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(() => EnqueueRender(frame));
                return;
            }

            if (frame is not null)
            {
                _pendingFrame = frame;
            }

            if (Interlocked.Exchange(ref _renderQueued, 1) == 1)
            {
                return;
            }

            _dispatcher.BeginInvoke(RenderLatest, DispatcherPriority.Background);
        }

        private void RenderLatest()
        {
            _renderQueued = 0;
            var frame = _pendingFrame;
            _pendingFrame = null;

            if (frame is not null)
            {
                EnsureBitmap(frame);
                if (ImagePixelConverter.TryConvertToBgra32(frame, GetConvertBuffer(frame)))
                {
                    _currentFrame = frame;
                    _bitmap!.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), GetConvertBuffer(frame), frame.Width * 4, 0);
                }

                if (!_fittedOnce)
                {
                    // 首帧自动适应窗口；之后由用户接管（手动缩放/平移后不再自动重铺）。
                    _fittedOnce = true;
                    FitToWindow();
                }
            }

            UpdateInfoBar();

            // 光标仍停在画面上时，新帧到了要重算像素读数（否则读数停留在旧帧的值）。
            if (_lastMouseScreen is { } mouse && _presenter is not null)
            {
                var (ix, iy) = _viewport.ScreenToImage(mouse.X, mouse.Y);
                UpdateMouseInfo(mouse, (int)Math.Floor(ix), (int)Math.Floor(iy));
            }

            InvalidatePresenter();
        }

        private byte[] GetConvertBuffer(ImageFrame frame)
        {
            int required = frame.Width * frame.Height * 4;
            if (_convertBuffer is null || _convertBuffer.Length < required)
            {
                _convertBuffer = new byte[required];
            }

            return _convertBuffer;
        }

        private void EnsureBitmap(ImageFrame frame)
        {
            if (_bitmap is null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
            {
                _bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null);
                if (_presenter is not null)
                {
                    _presenter.Bitmap = _bitmap;
                }
            }
        }

        private void InvalidatePresenter() => _presenter?.InvalidateVisual();

        // ============ 视图操作 ============

        private double ImageWidth => _currentFrame?.Width ?? 0;
        private double ImageHeight => _currentFrame?.Height ?? 0;
        private double ViewW => _presenter?.ActualWidth ?? 0;
        private double ViewH => _presenter?.ActualHeight ?? 0;

        private void ApplyView(Action<ImageViewerViewport> action)
        {
            if (ImageWidth <= 0 || ImageHeight <= 0 || ViewW <= 0 || ViewH <= 0)
            {
                return;
            }

            action(_viewport);
            OnViewChanged();
        }

        private void OnViewChanged()
        {
            _viewport.SetScaleRange(MinZoom, MaxZoom);
            SetValue(CurrentZoomPropertyKey, Math.Round(_viewport.Scale * 100, 1));
            InvalidatePresenter();
        }

        private void ToggleFitOrActual()
        {
            // 双击切换：当前已是 1:1 → 适应窗口；否则 → 实际大小。
            if (Math.Abs(_viewport.Scale - 1.0) < 0.001)
            {
                FitToWindow();
            }
            else
            {
                ActualSize();
            }
        }

        // ============ 鼠标交互 ============

        private double ScreenToleranceToImage => 6.0 / Math.Max(_viewport.Scale, 0.0001);

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // 滚轮缩放始终有效，以光标为锚。
            var p = e.GetPosition(_presenter);
            _autoFit = false;
            _viewport.ZoomAt(p.X, p.Y, e.Delta > 0 ? 1.25 : 0.8);
            OnViewChanged();
            e.Handled = true;
            base.OnMouseWheel(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Focus();
            var p = e.GetPosition(_presenter);

            if (e.ClickCount == 2 && _dragMode == DragMode.None)
            {
                ToggleFitOrActual();
                e.Handled = true;
                return;
            }

            if (_presenter is null)
            {
                return;
            }

            _autoFit = false;
            var (ix, iy) = _viewport.ScreenToImage(p.X, p.Y);
            double tolerance = ScreenToleranceToImage;
            _dragStartScreen = p;
            _dragStartImage = new Point(ix, iy);

            // 1) 右键菜单已选定下一笔（标注形状 / ROI 新框）→ 优先落笔（否则会被框的拖动劫持）。
            if (_pendingRoiDraw)
            {
                _pendingRoiDraw = false;
                _dragMode = DragMode.DrawingRoi;
                _roiDragStartRect = RoiRect; // 轻点未成框时恢复用
                _presenter.CaptureMouse();
                UpdateCursor();
                base.OnMouseLeftButtonDown(e);
                return;
            }

            if (_pendingAnnotationKind is not null)
            {
                _dragMode = DragMode.DrawingShape;
                var kind = _pendingAnnotationKind.Value;
                _draftShape = kind == ViewerShapeKind.Line
                    ? ViewerShape.Line(ix, iy, ix, iy)
                    : kind == ViewerShapeKind.Ellipse
                        ? ViewerShape.Ellipse(ix, iy, 0, 0)
                        : ViewerShape.Rectangle(ix, iy, 0, 0);
                _presenter.CaptureMouse();
                InvalidatePresenter();
                base.OnMouseLeftButtonDown(e);
                return;
            }

            // 2) 命中标注 → 选中并可拖动
            var shape = HitAnnotation(ix, iy, tolerance);
            if (shape is not null)
            {
                _selectedShape = shape;
                _dragMode = DragMode.MovingShape;
                _presenter.CaptureMouse();
                InvalidatePresenter();
                e.Handled = true;
                return;
            }

            _selectedShape = null;

            // 3) 命中 ROI：角点缩放，内部拖动——按在框上就操作框，不平移。
            int corner = HitRoiCorner(p, tolerance);
            if (corner >= 0)
            {
                _dragMode = DragMode.ResizingRoi;
                _resizeCorner = corner;
                _roiDragStartRect = RoiRect;
                _presenter.CaptureMouse();
            }
            else if (RoiRect.HasArea && PointInRect(RoiRect, ix, iy, tolerance))
            {
                _dragMode = DragMode.MovingRoi;
                _roiDragStartRect = RoiRect;
                _presenter.CaptureMouse();
            }
            else
            {
                // 4) 空白处按下 → 平移画面（左键的默认语义，无需切换工具）。
                _dragMode = DragMode.Panning;
                _presenter.CaptureMouse();
            }

            InvalidatePresenter();
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_presenter is null)
            {
                return;
            }

            var p = e.GetPosition(_presenter);
            _lastMouseScreen = p;
            var (ix, iy) = _viewport.ScreenToImage(p.X, p.Y);

            switch (_dragMode)
            {
                case DragMode.Panning:
                    _viewport.Pan(p.X - _dragStartScreen.X, p.Y - _dragStartScreen.Y);
                    _dragStartScreen = p;
                    OnViewChanged();
                    break;

                case DragMode.DrawingRoi:
                {
                    var (x2, y2) = _viewport.ScreenToImage(p.X, p.Y);
                    RoiRect = NormalizeRect(_dragStartImage.X, _dragStartImage.Y, x2, y2);
                    break;
                }

                case DragMode.DrawingShape when _draftShape is { } draft:
                {
                    var (x2, y2) = _viewport.ScreenToImage(p.X, p.Y);
                    if (draft.Kind == ViewerShapeKind.Line)
                    {
                        draft.SetLineEnd(x2, y2);
                    }
                    else
                    {
                        draft.SetBounds(_dragStartImage.X, _dragStartImage.Y, x2 - _dragStartImage.X, y2 - _dragStartImage.Y);
                    }

                    InvalidatePresenter();
                    break;
                }

                case DragMode.MovingRoi:
                {
                    var (x2, y2) = _viewport.ScreenToImage(p.X, p.Y);
                    double dx = x2 - _dragStartImage.X;
                    double dy = y2 - _dragStartImage.Y;
                    RoiRect = new Int32Rect(
                        (int)Math.Round(_roiDragStartRect.X + dx),
                        (int)Math.Round(_roiDragStartRect.Y + dy),
                        _roiDragStartRect.Width,
                        _roiDragStartRect.Height);
                    break;
                }

                case DragMode.ResizingRoi:
                {
                    var (x2, y2) = _viewport.ScreenToImage(p.X, p.Y);
                    RoiRect = ResizeRoiFromCorner(_roiDragStartRect, _resizeCorner, x2, y2);
                    break;
                }

                case DragMode.MovingShape when _selectedShape is { } shape:
                {
                    var (x2, y2) = _viewport.ScreenToImage(p.X, p.Y);
                    shape.Move(x2 - _dragStartImage.X, y2 - _dragStartImage.Y);
                    _dragStartImage = new Point(x2, y2);
                    InvalidatePresenter();
                    break;
                }
            }

            UpdateMouseInfo(p, (int)Math.Floor(ix), (int)Math.Floor(iy));
            UpdateCursor();
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            switch (_dragMode)
            {
                case DragMode.DrawingRoi:
                    // 有效拖拽（≥2 图像像素）才落 ROI；轻点不破坏已有 ROI。
                    if (RoiRect.Width < 2 || RoiRect.Height < 2)
                    {
                        RoiRect = _roiDragStartRect.HasArea ? _roiDragStartRect : Int32Rect.Empty;
                    }

                    break;

                case DragMode.DrawingShape when _draftShape is { } draft:
                    if (IsShapeBigEnough(draft))
                    {
                        Annotations.Add(draft);
                        _selectedShape = draft;
                    }

                    _draftShape = null;
                    _pendingAnnotationKind = null; // 一笔一收
                    UpdateCursor();
                    break;
            }

            if (_dragMode != DragMode.None)
            {
                _dragMode = DragMode.None;
                _presenter?.ReleaseMouseCapture();
                InvalidatePresenter();
            }

            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _lastMouseScreen = null;
            SetValue(MouseImagePositionPropertyKey, new Point(-1, -1));
            SetValue(PixelValueTextPropertyKey, string.Empty);
            SetInfoText(_infoPosition, string.Empty);
            SetInfoText(_infoPixel, string.Empty);
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete when _selectedShape is { } shape:
                    Annotations.Remove(shape);
                    _selectedShape = null;
                    InvalidatePresenter();
                    e.Handled = true;
                    break;

                case Key.Escape when _pendingAnnotationKind is not null || _pendingRoiDraw:
                    _pendingAnnotationKind = null;
                    _pendingRoiDraw = false;
                    UpdateCursor();
                    e.Handled = true;
                    break;
            }

            base.OnKeyDown(e);
        }

        // ============ 命中测试辅助 ============

        private ViewerShape? HitAnnotation(double ix, double iy, double tolerance)
        {
            var annotations = Annotations;
            if (annotations is null)
            {
                return null;
            }

            for (int i = annotations.Count - 1; i >= 0; i--) // 后画的在上层，优先命中
            {
                if (annotations[i].HitTest(ix, iy, tolerance))
                {
                    return annotations[i];
                }
            }

            return null;
        }

        private int HitRoiCorner(Point screenPoint, double imageTolerance)
        {
            if (!RoiRect.HasArea)
            {
                return -1;
            }

            var (ix, iy) = _viewport.ScreenToImage(screenPoint.X, screenPoint.Y);
            double t = imageTolerance;

            (double cx, double cy)[] corners =
            {
                (RoiRect.X, RoiRect.Y),
                (RoiRect.X + RoiRect.Width, RoiRect.Y),
                (RoiRect.X, RoiRect.Y + RoiRect.Height),
                (RoiRect.X + RoiRect.Width, RoiRect.Y + RoiRect.Height),
            };

            for (int i = 0; i < corners.Length; i++)
            {
                if (Math.Abs(ix - corners[i].cx) <= t && Math.Abs(iy - corners[i].cy) <= t)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool PointInRect(Int32Rect rect, double x, double y, double tolerance)
            => x >= rect.X - tolerance && x <= rect.X + rect.Width + tolerance
            && y >= rect.Y - tolerance && y <= rect.Y + rect.Height + tolerance;

        private static Int32Rect NormalizeRect(double x1, double y1, double x2, double y2)
        {
            double x = Math.Min(x1, x2);
            double y = Math.Min(y1, y2);
            return new Int32Rect(
                (int)Math.Round(x),
                (int)Math.Round(y),
                (int)Math.Round(Math.Abs(x2 - x1)),
                (int)Math.Round(Math.Abs(y2 - y1)));
        }

        /// <summary>按被拖的角缩放 ROI：对角固定，由起点矩形与当前点重算。</summary>
        private static Int32Rect ResizeRoiFromCorner(Int32Rect start, int corner, double x, double y)
        {
            (double fixedX, double fixedY) = corner switch
            {
                0 => (start.X + start.Width, start.Y + start.Height), // 拖左上角 → 右下角固定
                1 => (start.X, start.Y + start.Height),               // 拖右上角 → 左下角固定
                2 => (start.X + start.Width, start.Y),                // 拖左下角 → 右上角固定
                _ => (start.X, start.Y),                              // 拖右下角 → 左上角固定
            };

            return NormalizeRect(fixedX, fixedY, x, y);
        }

        private static bool IsShapeBigEnough(ViewerShape shape) => shape.Kind switch
        {
            ViewerShapeKind.Line => Math.Abs(shape.LineX2 - shape.X) + Math.Abs(shape.LineY2 - shape.Y) >= 2,
            _ => shape.Width >= 2 && shape.Height >= 2,
        };

        private void UpdateCursor()
        {
            if (_pendingAnnotationKind is not null || _pendingRoiDraw)
            {
                Cursor = Cursors.Cross;
                return;
            }

            Cursor = _dragMode switch
            {
                DragMode.Panning or DragMode.MovingShape or DragMode.MovingRoi => Cursors.SizeAll,
                DragMode.ResizingRoi => Cursors.SizeNWSE,
                DragMode.DrawingRoi or DragMode.DrawingShape => Cursors.Cross,
                _ => Cursors.Arrow,
            };
        }

        // ============ 信息条 ============

        private void UpdateMouseInfo(Point screenPoint, int imageX, int imageY)
        {
            SetValue(MouseImagePositionPropertyKey, new Point(imageX, imageY));

            string positionText = imageX >= 0
                ? $"X: {imageX}, Y: {imageY}"
                : string.Empty;
            SetInfoText(_infoPosition, positionText);

            string pixelText = string.Empty;
            if (imageX >= 0 && ImagePixelConverter.TryGetDisplayColor(_currentFrame, imageX, imageY, out byte r, out byte g, out byte b))
            {
                pixelText = _currentFrame!.PixelFormat == ImagePixelFormat.Mono8 || _currentFrame.PixelFormat == ImagePixelFormat.Mono16
                    ? $"灰度: {r}"
                    : $"R: {r}  G: {g}  B: {b}";
            }

            SetValue(PixelValueTextPropertyKey, pixelText);
            SetInfoText(_infoPixel, pixelText);
        }

        private void UpdateInfoBar()
        {
            SetInfoText(_infoZoom, $"{Math.Round(_viewport.Scale * 100, 1):0.#}%");
            SetValue(CurrentZoomPropertyKey, Math.Round(_viewport.Scale * 100, 1));
        }

        private static void SetInfoText(TextBlock? target, string text)
        {
            if (target is not null)
            {
                target.Text = text;
            }
        }

        // ============ 矢量层渲染（由 ImageViewerPresenter.OnRender 委托调用） ============

        internal void RenderVectorLayer(DrawingContext dc, ImageViewerPresenter presenter)
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(presenter).PixelsPerDip;

            if (_currentFrame is null)
            {
                // 空态：明确告诉用户"这里还没有图"，避免和浅色主题背景混成一片。
                var hint = CreateFormattedText("暂无图像", CreateOpacityBrush(Foreground ?? Brushes.Gray, 0.5), 14, pixelsPerDip);
                dc.DrawText(hint, new Point((ViewW - hint.Width) / 2, (ViewH - hint.Height) / 2));
                return;
            }

            if (IsCenterCrosshair)
            {
                DrawCenterCrosshair(dc, pixelsPerDip);
            }

            if (RoiRect.HasArea)
            {
                DrawRoi(dc);
            }

            DrawShapeCollection(dc, Results, ResultBrush, drawLabel: true, pixelsPerDip, selected: null);
            DrawShapeCollection(dc, Annotations, AnnotationBrush, drawLabel: false, pixelsPerDip, _selectedShape);

            if (_draftShape is not null)
            {
                DrawShape(dc, _draftShape, AnnotationBrush, dashed: true);
            }
        }

        private void DrawCenterCrosshair(DrawingContext dc, double pixelsPerDip)
        {
            var (cx, cy) = _viewport.ImageToScreen(ImageWidth / 2, ImageHeight / 2);
            var brush = CenterCrosshairBrush ?? Brushes.Green;
            var pen = new Pen(CreateOpacityBrush(brush, 0.8), 1);
            double w = ViewW;
            double h = ViewH;
            dc.DrawLine(pen, new Point(0, cy), new Point(w, cy));
            dc.DrawLine(pen, new Point(cx, 0), new Point(cx, h));
        }

        private void DrawRoi(DrawingContext dc)
        {
            var rect = ShapeToScreenRect(RoiRect.X, RoiRect.Y, RoiRect.Width, RoiRect.Height);
            var pen = new Pen(RoiBrush, 1.5);
            dc.DrawRectangle(null, pen, rect);

            // 四角手柄（屏幕尺寸固定，缩放时不变形）
            double handle = 7;
            foreach (var corner in new[]
                     {
                         new Point(rect.Left, rect.Top),
                         new Point(rect.Right, rect.Top),
                         new Point(rect.Left, rect.Bottom),
                         new Point(rect.Right, rect.Bottom),
                     })
            {
                var handleRect = new Rect(corner.X - handle / 2, corner.Y - handle / 2, handle, handle);
                dc.DrawRectangle(RoiBrush, new Pen(Brushes.Black, 1), handleRect);
            }
        }

        private void DrawShapeCollection(
            DrawingContext dc,
            ObservableCollection<ViewerShape>? shapes,
            Brush brush,
            bool drawLabel,
            double pixelsPerDip,
            ViewerShape? selected)
        {
            if (shapes is null)
            {
                return;
            }

            foreach (var shape in shapes)
            {
                DrawShape(dc, shape, brush, dashed: false);
                if (drawLabel && !string.IsNullOrEmpty(shape.Label))
                {
                    DrawLabel(dc, shape, shape.Label!, brush, pixelsPerDip);
                }
            }

            if (selected is not null && shapes.Contains(selected))
            {
                DrawShape(dc, selected, Brushes.White, dashed: true);
            }
        }

        private void DrawShape(DrawingContext dc, ViewerShape shape, Brush brush, bool dashed)
        {
            var pen = dashed
                ? new Pen(CreateOpacityBrush(Brushes.White, 0.9), 1) { DashStyle = DashStyles.Dash }
                : new Pen(brush, 2);

            switch (shape.Kind)
            {
                case ViewerShapeKind.Rectangle:
                    dc.DrawRectangle(null, pen, ShapeToScreenRect(shape.X, shape.Y, shape.Width, shape.Height));
                    break;

                case ViewerShapeKind.Ellipse:
                    dc.DrawEllipse(null, pen,
                        new Point((shape.X + shape.Width / 2) * _viewport.Scale - _viewport.OffsetX,
                                  (shape.Y + shape.Height / 2) * _viewport.Scale - _viewport.OffsetY),
                        shape.Width / 2 * _viewport.Scale,
                        shape.Height / 2 * _viewport.Scale);
                    break;

                case ViewerShapeKind.Line:
                {
                    (double x1, double y1) = _viewport.ImageToScreen(shape.X, shape.Y);
                    (double x2, double y2) = _viewport.ImageToScreen(shape.LineX2, shape.LineY2);
                    dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
                    break;
                }
            }
        }

        private void DrawLabel(DrawingContext dc, ViewerShape shape, string label, Brush brush, double pixelsPerDip)
        {
            // 标签贴形状左上角，带底色块保证任何画面下可读。
            var text = CreateFormattedText(label, Brushes.White, 12, pixelsPerDip);
            (double x, double y) = _viewport.ImageToScreen(shape.X, shape.Y);
            var background = new Rect(x, y - text.Height - 4, text.Width + 6, text.Height + 4);
            dc.DrawRoundedRectangle(brush, null, background, 2, 2);
            dc.DrawText(text, new Point(x + 3, y - text.Height - 2));
        }

        private Rect ShapeToScreenRect(double x, double y, double width, double height)
        {
            (double sx, double sy) = _viewport.ImageToScreen(x, y);
            return new Rect(sx, sy, Math.Max(width * _viewport.Scale, 1), Math.Max(height * _viewport.Scale, 1));
        }

        // ============ 右键标注菜单 ============

        private ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();

            // 绘制组：选定后下一笔左键拖拽落框，Esc 取消
            var drawRoi = new MenuItem { Header = "画 ROI" };
            drawRoi.Click += (s, e) => BeginRoiDraw();
            var rectItem = new MenuItem { Header = "画矩形标注" };
            rectItem.Click += (s, e) => BeginAnnotation(ViewerShapeKind.Rectangle);
            var lineItem = new MenuItem { Header = "画直线标注" };
            lineItem.Click += (s, e) => BeginAnnotation(ViewerShapeKind.Line);
            var ellipseItem = new MenuItem { Header = "画圆形标注" };
            ellipseItem.Click += (s, e) => BeginAnnotation(ViewerShapeKind.Ellipse);
            menu.Items.Add(drawRoi);
            menu.Items.Add(rectItem);
            menu.Items.Add(lineItem);
            menu.Items.Add(ellipseItem);

            menu.Items.Add(new Separator());

            var clearAnnotations = new MenuItem { Header = "清除全部标注" };
            clearAnnotations.Click += (s, e) => ClearAnnotations();
            menu.Items.Add(clearAnnotations);

            var clearRoi = new MenuItem { Header = "清除 ROI" };
            clearRoi.Click += (s, e) => ClearRoi();
            menu.Items.Add(clearRoi);

            menu.Items.Add(new Separator());

            var fitItem = new MenuItem { Header = "适应窗口" };
            fitItem.Click += (s, e) => FitToWindow();
            var actualItem = new MenuItem { Header = "实际大小 (1:1)" };
            actualItem.Click += (s, e) => ActualSize();
            menu.Items.Add(fitItem);
            menu.Items.Add(actualItem);

            return menu;
        }

        /// <summary>选定下一笔标注的形状；下一次左键拖拽落笔，Esc 取消，画完一笔即收。</summary>
        private void BeginAnnotation(ViewerShapeKind kind)
        {
            _pendingAnnotationKind = kind;
            _pendingRoiDraw = false;
            _selectedShape = null;
            UpdateCursor();
        }

        /// <summary>选定下一笔画 ROI 工作矩形（单个，替换旧值）；下一次左键拖拽落框，Esc 取消。</summary>
        private void BeginRoiDraw()
        {
            _pendingRoiDraw = true;
            _pendingAnnotationKind = null;
            _selectedShape = null;
            UpdateCursor();
        }

        // ============ 杂项 ============

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static Brush CreateOpacityBrush(Brush source, double opacity)
        {
            var brush = source.Clone();
            brush.Opacity = opacity;
            return brush;
        }

        private FormattedText CreateFormattedText(string text, Brush brush, double size, double pixelsPerDip)
            => new(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                size,
                brush,
                pixelsPerDip);
    }
}
