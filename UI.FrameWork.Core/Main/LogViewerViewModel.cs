using EFCore.Repository;
using Log.Domain;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UI.FrameWork.Core.Events;
using UI.FrameWork.Core.Main.View;

namespace UI.FrameWork.Core.Main
{
    /// <summary>
    /// 日志查看器 ViewModel — 分页展示 SerilogHistory 数据，支持按时间范围筛选
    /// </summary>
    public class LogViewerViewModel : BaseViewModel, INavigationAware
    {
        private readonly ISerilogService _serilogService;

        public LogViewerViewModel(ISerilogService serilogService, IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _serilogService = serilogService;

            PrevPageCommand = new DelegateCommand(async () => await GoToPage(CurrentPage - 1),
                () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(async () => await GoToPage(CurrentPage + 1),
                () => CurrentPage < TotalPages);
            FirstPageCommand = new DelegateCommand(async () => await GoToPage(1),
                () => CurrentPage > 1);
            LastPageCommand = new DelegateCommand(async () => await GoToPage(TotalPages),
                () => CurrentPage < TotalPages);
            QueryByDateCommand = new DelegateCommand(async () => await QueryByDateRange());
            ClearDateFilterCommand = new DelegateCommand(ClearDateFilter);

            // 初始化加载
            _ = LoadDataAsync();
        }

        #region 属性

        private ObservableCollection<SerilogHistory> _logEntries = new();
        public ObservableCollection<SerilogHistory> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    RaisePropertyChanged(nameof(PageInfo));
                    RefreshCommandStates();
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    RaisePropertyChanged(nameof(TotalPages));
                    RaisePropertyChanged(nameof(PageInfo));
                    RefreshCommandStates();
                }
            }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadDataAsync();
                }
            }
        }

        public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        public string PageInfo => TotalPages == 0
            ? "暂无数据"
            : $"第 {CurrentPage} / {TotalPages} 页，共 {TotalCount} 条";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>可选页面大小</summary>
        public static int[] PageSizeOptions { get; } = { 10, 20, 50, 100 };

        private DateTime? _startDate;
        /// <summary>筛选起始日期</summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    RaisePropertyChanged(nameof(IsDateFilterActive));
                    RaisePropertyChanged(nameof(FilterInfo));
                }
            }
        }

        private DateTime? _endDate;
        /// <summary>筛选结束日期</summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    RaisePropertyChanged(nameof(IsDateFilterActive));
                    RaisePropertyChanged(nameof(FilterInfo));
                }
            }
        }

        /// <summary>是否启用了日期筛选</summary>
        public bool IsDateFilterActive => StartDate.HasValue && EndDate.HasValue;

        /// <summary>筛选状态提示文字</summary>
        public string FilterInfo => IsDateFilterActive
            ? $"🔍 {StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}"
            : "";

        #endregion

        #region 命令

        public DelegateCommand PrevPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }
        public DelegateCommand QueryByDateCommand { get; }
        public DelegateCommand ClearDateFilterCommand { get; }

        #endregion

        #region 方法

        private async Task GoToPage(int page)
        {
            if (page < 1) return;
            if (TotalPages > 0 && page > TotalPages) return;
            CurrentPage = page;
            await LoadDataAsync();
        }

        /// <summary>
        /// 按日期范围查询，重置到第一页
        /// </summary>
        private async Task QueryByDateRange()
        {
            if (StartDate == null || EndDate == null) return;
            if (StartDate.Value > EndDate.Value)
            {
                var temp = StartDate;
                StartDate = EndDate;
                EndDate = temp;
            }

            CurrentPage = 1;
            await LoadDataAsync();
        }

        /// <summary>
        /// 清除日期筛选，恢复全量查询
        /// </summary>
        private void ClearDateFilter()
        {
            StartDate = null;
            EndDate = null;
            CurrentPage = 1;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var parameter = new PageParameter
                {
                    PageNum = CurrentPage,
                    PageSize = PageSize
                };

                PagedResult<SerilogHistory> result;

                if (IsDateFilterActive)
                {
                    // 将日期转为字符串比较（Serilog Timestamp 格式天然支持字典序）
                    var start = StartDate.Value.ToString("yyyy-MM-dd");
                    var end = EndDate.Value.AddDays(1).ToString("yyyy-MM-dd");

                    result = await _serilogService.GetPageAsync(
                        x => string.Compare(x.Timestamp, start) >= 0
                          && string.Compare(x.Timestamp, end) < 0,
                        parameter);
                }
                else
                {
                    result = await _serilogService.GetPageAsync(parameter);
                }

                TotalCount = result.Total;
                LogEntries = new ObservableCollection<SerilogHistory>(result.Data);

                // 如果当前页超出范围，回退到最后一页
                if (TotalPages > 0 && CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    await LoadDataAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载日志失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshCommandStates()
        {
            PrevPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            FirstPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<LayoutModeChangedEvent>().Publish(true);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<LayoutModeChangedEvent>().Publish(false);
        }

        #endregion
    }
}
