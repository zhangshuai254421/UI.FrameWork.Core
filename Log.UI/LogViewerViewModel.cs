using EFCore.Repository;
using Framework.Core.Common;
using Log.Infrastructure.Contracts;
using Prism.Commands;
using PrismUI.Core;
using Log.Infrastructure;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Log.UI
{
    /// <summary>
    /// 日志查看器 ViewModel — 分页展示日志（DTO 只读投影），支持按时间范围筛选（ADR 0002 迁移第④步）
    /// </summary>
    public class LogViewerViewModel : BaseViewModel, INavigationAware
    {
        /// <summary>日志服务：用于分页查询日志数据。</summary>
        private readonly SerilogService _serilogService;

        /// <summary>注入日志服务，初始化分页与筛选命令后立即加载首页数据。</summary>
        public LogViewerViewModel(SerilogService serilogService)
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

        /// <summary>当前页日志列表 backing 字段。</summary>
        private ObservableCollection<LogEntryDto> _logEntries = new();
        /// <summary>当前页日志列表。</summary>
        public ObservableCollection<LogEntryDto> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        /// <summary>当前页码 backing 字段。</summary>
        private int _currentPage = 1;
        /// <summary>当前页码，从 1 开始；变化时刷新分页信息与命令可用状态。</summary>
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

        /// <summary>日志总条数 backing 字段。</summary>
        private int _totalCount;
        /// <summary>符合条件的日志总条数；变化时刷新总页数与分页信息。</summary>
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

        /// <summary>每页条数 backing 字段。</summary>
        private int _pageSize = 100;
        /// <summary>每页条数，默认 100；变化时回到第 1 页并重新加载。</summary>
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

        /// <summary>总页数；无数据时为 0。</summary>
        public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>分页状态提示文字，如"第 1 / 5 页，共 500 条"。</summary>
        public string PageInfo => TotalPages == 0
            ? "暂无数据"
            : $"第 {CurrentPage} / {TotalPages} 页，共 {TotalCount} 条";

        /// <summary>加载中标志 backing 字段。</summary>
        private bool _isLoading;
        /// <summary>是否正在加载日志，用于界面等待状态展示。</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>可选页面大小</summary>
        public static int[] PageSizeOptions { get; } = { 10, 20, 50, 100 };

        /// <summary>筛选起始日期 backing 字段。</summary>
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

        /// <summary>筛选结束日期 backing 字段。</summary>
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

        /// <summary>跳转上一页命令；已在第一页时不可用。</summary>
        public DelegateCommand PrevPageCommand { get; }
        /// <summary>跳转下一页命令；已在最后一页时不可用。</summary>
        public DelegateCommand NextPageCommand { get; }
        /// <summary>跳转第一页命令；已在第一页时不可用。</summary>
        public DelegateCommand FirstPageCommand { get; }
        /// <summary>跳转最后一页命令；已在最后一页时不可用。</summary>
        public DelegateCommand LastPageCommand { get; }
        /// <summary>按日期范围执行查询的命令。</summary>
        public DelegateCommand QueryByDateCommand { get; }
        /// <summary>清除日期筛选并恢复全量查询的命令。</summary>
        public DelegateCommand ClearDateFilterCommand { get; }

        #endregion

        #region 方法

        /// <summary>跳转到指定页并重新加载数据；页码越界时忽略。</summary>
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

        /// <summary>按当前页码、页大小与日期筛选加载日志；失败时仅记录调试输出，不抛异常。</summary>
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

                var result = await _serilogService.GetLogPageAsync(
                    parameter,
                    IsDateFilterActive ? StartDate : null,
                    IsDateFilterActive ? EndDate : null);

                TotalCount = result.Total;
                LogEntries = new ObservableCollection<LogEntryDto>(result.Data);

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

        /// <summary>刷新全部分页命令的可用状态。</summary>
        private void RefreshCommandStates()
        {
            PrevPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            FirstPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
        }

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        #endregion
    }
}
