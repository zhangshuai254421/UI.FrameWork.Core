using EFCore.Repository;
using Log.Domain;
using Prism.Commands;
using System.Collections.ObjectModel;

namespace UI.FrameWork.Core.Main
{
    /// <summary>
    /// 日志查看器 ViewModel — 分页展示 SerilogHistory 数据
    /// </summary>
    public class LogViewerViewModel : BindableBase
    {
        private readonly ISerilogService _serilogService;

        public LogViewerViewModel(ISerilogService serilogService)
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
                    RaisePropertyChanged(nameof(TotalPages));
                    RaisePropertyChanged(nameof(PageInfo));
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

        #endregion

        #region 命令

        public DelegateCommand PrevPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        #endregion

        #region 方法

        private async Task GoToPage(int page)
        {
            if (page < 1 || (TotalPages > 0 && page > TotalPages)) return;
            CurrentPage = page;
            await LoadDataAsync();
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

                var result = await _serilogService.GetPageAsync(parameter);

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
                System.Diagnostics.Debug.WriteLine($"加载日志失败: {ex.Message}");
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

        #endregion
    }
}
