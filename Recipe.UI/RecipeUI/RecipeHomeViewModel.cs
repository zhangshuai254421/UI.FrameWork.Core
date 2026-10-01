using Framework.Core.Common;
using Microsoft.Extensions.Logging;
using PrismUI.Core;
using Recipe.Infrastructure;
using Recipe.Infrastructure.Contracts;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Recipe.UI.RecipeUI
{
    /// <summary>
    /// 配方管理主页视图模型：负责文件夹与配方列表的展示、增删改、复制、应用切换，
    /// 写操作全部委托 RecipeService，本页只靠订阅配方变更事件整条刷新。
    /// </summary>
    public class RecipeHomeViewModel : BaseViewModel, INavigationAware
    {
        #region 字段

        /// <summary>配方服务，负责配方的增删改查与事件推送。</summary>
        private readonly RecipeService _recipeService;
        /// <summary>Prism 弹框服务，用于重命名、复制、删除确认等对话框。</summary>
        private readonly IDialogService _dialogService;
        /// <summary>Prism 导航服务，用于确认配方后跳转页面。</summary>
        private readonly INavigationService _navigationService;

        /// <summary>重命名命令的后备字段（懒加载）。</summary>
        private DelegateCommand _renameCmd = null!;
        /// <summary>复制命令的后备字段（懒加载）。</summary>
        private DelegateCommand _copyRecipeCmd = null!;
        /// <summary>删除配方命令的后备字段（懒加载）。</summary>
        private DelegateCommand _deleteRecipeCmd = null!;
        /// <summary>应用配方命令的后备字段（懒加载）。</summary>
        private DelegateCommand _applyRecipeCmd = null!;
        /// <summary>新建文件夹命令的后备字段（懒加载）。</summary>
        private DelegateCommand _newGroupCmd = null!;
        /// <summary>删除文件夹命令的后备字段（懒加载）。</summary>
        private DelegateCommand _deleteGroupCmd = null!;

        /// <summary>配方列表属性的后备字段。</summary>
        private ObservableCollection<RecipeListItemDto> recipes = new ObservableCollection<RecipeListItemDto>();
        /// <summary>文件夹名列表属性的后备字段。</summary>
        private ObservableCollection<string> _recipeWithGroupName = new ObservableCollection<string>();
        /// <summary>当前分组下配方名列表属性的后备字段。</summary>
        private ObservableCollection<string> _recipeWithRecipeName = new ObservableCollection<string>();

        /// <summary>当前选中分组名属性的后备字段。</summary>
        private string _selectGroupName = null!;
        /// <summary>当前选中配方名属性的后备字段。</summary>
        private string _selectRecipeName = null!;
        /// <summary>当前分组下配方数量属性的后备字段。</summary>
        private int _recipeShowCount;
        /// <summary>全部配方数量属性的后备字段。</summary>
        private int _recipeCount;

        /// <summary>按分组记忆的配方选中缓存：键为分组名，值为该分组最近一次选中的配方名。</summary>
        public Dictionary<string, string> RecipeSelectCache = new Dictionary<string, string>();

        #endregion

        #region 构造函数

        /// <summary>构造函数：注入配方服务、弹框服务与导航服务。</summary>
        public RecipeHomeViewModel(RecipeService recipeService, IDialogService dialogService, INavigationService navigationService)
        {
            _recipeService = recipeService;
            _dialogService = dialogService;
            _navigationService = navigationService;
        }

        #endregion

        #region 属性

        /// <summary>配方数据源（DTO 投影，整条替换刷新，ADR 0002 规则 5）。</summary>
        public ObservableCollection<RecipeListItemDto> Recipes
        {
            get => recipes;
            set => SetProperty(ref recipes, value);
        }

        /// <summary>文件夹名列表（左侧分组下拉数据源）。</summary>
        public ObservableCollection<string> RecipeWithGroupName
        {
            get => _recipeWithGroupName;
            set => SetProperty(ref _recipeWithGroupName, value);
        }

        /// <summary>当前分组下的配方名列表（配方下拉数据源）。</summary>
        public ObservableCollection<string> RecipeWithRecipeName
        {
            get => _recipeWithRecipeName;
            set => SetProperty(ref _recipeWithRecipeName, value);
        }

        /// <summary>选择分组：切换后按缓存/新分组列表解析默认配方名。</summary>
        public string SelectGroupName
        {
            get => _selectGroupName;
            set
            {
                if (value == _selectGroupName) return;
                _selectGroupName = value;

                if (value != null && !RecipeSelectCache.ContainsKey(value))
                {
                    RecipeSelectCache.Add(value, string.Empty);
                }

                RaisePropertyChanged(nameof(SelectGroupName));
                RefreshRecipeNames();
            }
        }

        /// <summary>选择配方：记入按分组的选中缓存。</summary>
        public string SelectRecipeName
        {
            get => _selectRecipeName;
            set
            {
                if (value == _selectRecipeName) return;
                _selectRecipeName = value;

                if (RecipeSelectCache.ContainsKey(SelectGroupName) && value != null)
                {
                    RecipeSelectCache[SelectGroupName] = value;
                }
                RaisePropertyChanged(nameof(SelectRecipeName));
            }
        }

        /// <summary>当前分组下的配方数量（界面显示用）。</summary>
        public int RecipeShowCount
        {
            get => _recipeShowCount;
            set => SetProperty(ref _recipeShowCount, value);
        }

        /// <summary>全部配方总数（界面显示用）。</summary>
        public int RecipeCount
        {
            get => _recipeCount;
            set => SetProperty(ref _recipeCount, value);
        }

        #endregion

        #region 命令

        /// <summary>重命名选中配方：弹框取新名，service 落库并发变更事件，本页靠订阅刷新。</summary>
        public DelegateCommand RenameCmd => _renameCmd ?? new DelegateCommand(async () => await RenameAsync());

        /// <summary>复制选中配方：连同参数克隆为新配方。</summary>
        public DelegateCommand CopyRecipeCmd => _copyRecipeCmd ?? new DelegateCommand(async () => await CopyAsync());

        /// <summary>删除选中配方：确认后交 service 删除并发变更事件。</summary>
        public DelegateCommand DeleteRecipeCmd => _deleteRecipeCmd ?? new DelegateCommand(async () => await DeleteAsync());

        /// <summary>应用选中配方：写 RecipeManager.CurrentRecipeId 并广播配方切换事件。</summary>
        public DelegateCommand ApplyRecipeCmd => _applyRecipeCmd ?? new DelegateCommand(async () => await ApplyAsync());

        /// <summary>创建文件夹：弹框输入文件夹名，service 建占位配方并发变更事件。</summary>
        public DelegateCommand NewGroupCmd => _newGroupCmd ?? new DelegateCommand(async () => await CreateGroupAsync());

        /// <summary>删除选中文件夹：确认后连同其下配方一起删除。</summary>
        public DelegateCommand DeleteGroupCmd => _deleteGroupCmd ?? new DelegateCommand(async () => await DeleteGroupAsync());

        #endregion

        #region 方法

        /// <summary>回车确认命令：应用当前选中配方并跳转到配方上下文页。</summary>
        public override void EnterCommandExecute()
        {
            base.EnterCommandExecute();
            ApplyRecipeCmd.Execute();
            _navigationService.NavigateToAsync(ViewNames.RecipeContextView, RegionNames.MainRegion);
        }

        /// <summary>重命名流程：未选中配方直接返回；弹框取消或新名无效时忽略，否则交 service 落库。</summary>
        private async Task RenameAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            var dialogResult = await _dialogService.ShowDialogAsync(
                nameof(RenameRecipeDialogView),
                new DialogParameters { { "RecipeName", SelectRecipeName } });

            if (dialogResult.Result != ButtonResult.OK)
            {
                return; // Cancel 则什么都不做
            }

            var newRecipeName = dialogResult.Parameters.GetValue<string>("NewRecipeName");
            if (string.IsNullOrWhiteSpace(newRecipeName) || newRecipeName == SelectRecipeName)
            {
                return;
            }

            await _recipeService.RenameRecipeAsync(SelectGroupName, SelectRecipeName, newRecipeName);
        }

        /// <summary>复制流程：弹框取新配方名与目标文件夹，确认后交 service 克隆配方（未选中或取消时忽略）。</summary>
        private async Task CopyAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            var dialogResult = await _dialogService.ShowDialogAsync(
                nameof(CopyRecipeDialogView),
                new DialogParameters
                {
                    { "RecipeName", SelectRecipeName },
                    { "GroupName", SelectGroupName },
                    { "GroupNames", RecipeWithGroupName.ToList() }
                });

            if (dialogResult.Result != ButtonResult.OK)
            {
                return;
            }

            var newRecipeName = dialogResult.Parameters.GetValue<string>("NewRecipeName");
            var newGroupName = dialogResult.Parameters.GetValue<string>("NewGroupName");
            if (string.IsNullOrWhiteSpace(newRecipeName) || newRecipeName == SelectRecipeName)
            {
                return;
            }

            await _recipeService.CopyRecipeAsync(SelectGroupName, SelectRecipeName, newRecipeName, AppGlobals.MachineName, newGroupName);
        }

        /// <summary>删除流程：弹确认框，用户确认 Yes 后交 service 删除（未选中或取消时忽略）。</summary>
        private async Task DeleteAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不弹框
            }

            var result = await _dialogService.ShowDialogAsync(
                "ConfirmationDialog",
                new DialogParameters
                {
                    { "Title", "删除确认" },
                    { "Message", "确定要删除选中的用户吗？此操作不可撤销。" }
                });

            if (result.Result != ButtonResult.Yes)
            {
                return;
            }

            await _recipeService.DeleteRecipeAsync(SelectGroupName, SelectRecipeName);
        }

        /// <summary>应用流程：按当前分组与配方名查出配方，设为当前配方成功后广播配方切换事件。</summary>
        private async Task ApplyAsync()
        {
            Logger.LogInformation("ApplyRecipeCmd");
            if (string.IsNullOrWhiteSpace(SelectRecipeName))
            {
                return; // 没有选中配方，不执行
            }

            var list = await _recipeService.GetRecipeListAsync(AppGlobals.MachineName);
            var selected = list.FirstOrDefault(p => p.GroupName == SelectGroupName && p.RecipeName == SelectRecipeName);
            if (selected == null)
            {
                return;
            }

            if (await _recipeService.SetCurrentRecipeAsync(selected.Id))
            {
                EventAggregator.GetEvent<ChangeRecipeEvent>().Publish(SelectRecipeName);
            }
        }

        /// <summary>新建文件夹流程：弹框取文件夹名，交 service 创建占位配方，成功后选中新文件夹。</summary>
        private async Task CreateGroupAsync()
        {
            var dialogResult = await _dialogService.ShowDialogAsync(
                nameof(NewGroupDialogView),
                new DialogParameters());

            if (dialogResult.Result != ButtonResult.OK)
            {
                return;
            }

            var newGroupName = dialogResult.Parameters.GetValue<string>("NewGroupName");
            if (string.IsNullOrWhiteSpace(newGroupName))
            {
                return;
            }

            await _recipeService.CreateGroupAsync(newGroupName, AppGlobals.MachineName);

            // 创建后选中新文件夹；随后的变更事件刷新会把空配方列表带出来
            SelectGroupName = newGroupName;
        }

        /// <summary>删除文件夹流程：弹确认框，确认 Yes 后交 service 连同其下配方一起删除（未选中或取消时忽略）。</summary>
        private async Task DeleteGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectGroupName))
            {
                return; // 没有选中文件夹，不弹框
            }

            var result = await _dialogService.ShowDialogAsync(
                "ConfirmationDialog",
                new DialogParameters
                {
                    { "Title", "删除文件夹确认" },
                    { "Message", $"确定要删除文件夹“{SelectGroupName}”吗？其下所有配方将一并删除，此操作不可撤销。" }
                });

            if (result.Result != ButtonResult.Yes)
            {
                return;
            }

            await _recipeService.DeleteGroupAsync(SelectGroupName, AppGlobals.MachineName);
        }

        /// <summary>
        /// 全量刷新：文件夹列表与配方列表整条重查（DTO 投影），再按当前分组过滤配方名。
        /// 当前分组已被删除时，回退选中第一个分组。
        /// </summary>
        public async Task RefreshSelectGroupNameAsync()
        {
            Recipes = new ObservableCollection<RecipeListItemDto>(
                await _recipeService.GetRecipeListAsync(AppGlobals.MachineName));
            RecipeWithGroupName = new ObservableCollection<string>(
                await _recipeService.GetGroupListAsync(AppGlobals.MachineName));
            RecipeCount = Recipes.Count;

            if (SelectGroupName == null || !RecipeWithGroupName.Contains(SelectGroupName))
            {
                SelectGroupName = RecipeWithGroupName.FirstOrDefault(); // 触发 setter 过滤配方名
            }
            else
            {
                RefreshRecipeNames();
            }
        }

        /// <summary>
        /// 按当前分组在内存中过滤配方名，并解析默认选中项（缓存优先，失效回退首项）。
        /// </summary>
        private void RefreshRecipeNames()
        {
            RecipeWithRecipeName = new ObservableCollection<string>(
                Recipes.Where(p => p.GroupName == SelectGroupName).Select(p => p.RecipeName));
            RecipeShowCount = RecipeWithRecipeName.Count;

            var cached = RecipeSelectCache.TryGetValue(SelectGroupName ?? string.Empty, out var value) ? value : null;
            SelectRecipeName = cached != null && RecipeWithRecipeName.Contains(cached)
                ? cached
                : RecipeWithRecipeName.FirstOrDefault();
        }

        #endregion

        #region INavigationAware

        /// <summary>导航进入时订阅配方变更事件并全量刷新列表；加载失败只记录日志，不抛出。</summary>
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<RecipeChangedEvent>().Subscribe(OnRecipeChanged);
            try
            {
                await RefreshSelectGroupNameAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载配方列表失败");
            }
        }

        /// <summary>导航目标判定：始终复用本实例。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true; // 复用同一个 View 实例，避免重复创建
        }

        /// <summary>导航离开时取消订阅配方变更事件，避免泄漏。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<RecipeChangedEvent>().Unsubscribe(OnRecipeChanged);
        }

        /// <summary>service 写后推送的配方变更：整条重查刷新（ADR 0002 规则 4/5）。</summary>
        private async void OnRecipeChanged(RecipeChangedPayload payload)
        {
            try
            {
                await RefreshSelectGroupNameAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "响应配方变更事件失败");
            }
        }

        #endregion
    }
}
