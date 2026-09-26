using PrismUI.Core;
using Microsoft.Extensions.Logging;
using Recipe.Infrastructure;
using Recipe.Infrastructure.Contracts;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main.RecipeView
{
    /// <summary>
    /// 相机配置编辑页（ADR 0002 迁移第②步）：
    /// 进页面时经 service 查询投影加载编辑契约（Input），
    /// 行模型只是 Input 的 INPC 薄包装；点"保存"把 Input 原样交回 service 落库，
    /// 离开页面不保存即为取消——Input 随 VM 丢弃，数据库从未被触碰。
    /// </summary>
    public class RecipeContextViewModel : BaseViewModel, INavigationAware
    {
        #region 字段

        private readonly CameraConfigurationService cameraConfigurationService;
        private DelegateCommand _saveCmd = null!;
        private ObservableCollection<RecipeContextViewDto> recipeContextViewShowModels = new ObservableCollection<RecipeContextViewDto>();

        #endregion

        #region 构造函数

        public RecipeContextViewModel(CameraConfigurationService cameraConfigurationService)
        {
            this.cameraConfigurationService = cameraConfigurationService;
        }

        #endregion

        #region 属性

        public ObservableCollection<RecipeContextViewDto> RecipeContextViewDto
        {
            get => recipeContextViewShowModels;
            set => SetProperty(ref recipeContextViewShowModels, value);
        }

        #endregion

        #region 命令

        /// <summary>把编辑行的 Input 原样交回 service 落库；不保存离开页面即为取消。</summary>
        public DelegateCommand SaveCmd => _saveCmd ?? new DelegateCommand(async () => await SaveAsync());

        #endregion

        #region 方法

        private async Task SaveAsync()
        {
            try
            {
                var inputs = RecipeContextViewDto.Select(x => x.ToInput()).ToList();
                await cameraConfigurationService.SaveCameraNameEditAsync(inputs);
                Logger.LogInformation("相机配置已保存：{Count} 项", inputs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存相机配置失败");
            }
        }
        public override  void EnterCommandExecute()
        {
             _= SaveAsync();
        }

        private async Task LoadAsync()
        {
            var inputs = await cameraConfigurationService.GetCameraNameEditListAsync();
            RecipeContextViewDto = new ObservableCollection<RecipeContextViewDto>(inputs.Select(x => new RecipeContextViewDto(x)));
        }

        #endregion

        #region INavigationAware

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载相机配置失败");
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true; // 复用同一个 View 实例，避免重复创建
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 不保存即离开 = 取消（ADR 0002 规则 3）：Input 随 VM 丢弃，数据库从未被触碰。
        }

        #endregion
    }
}
