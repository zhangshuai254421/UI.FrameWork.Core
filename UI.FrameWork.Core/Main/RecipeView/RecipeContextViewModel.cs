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

        /// <summary>相机配置服务：查询编辑契约与保存落库。</summary>
        private readonly CameraConfigurationService cameraConfigurationService;
        /// <summary>保存命令的缓存实例（预留）。</summary>
        private DelegateCommand _saveCmd = null!;
        /// <summary>编辑行集合的后端字段。</summary>
        private ObservableCollection<RecipeContextViewDto> recipeContextViewShowModels = new ObservableCollection<RecipeContextViewDto>();

        #endregion

        #region 构造函数

        /// <summary>构造：注入相机配置服务。</summary>
        public RecipeContextViewModel(CameraConfigurationService cameraConfigurationService)
        {
            this.cameraConfigurationService = cameraConfigurationService;
        }

        #endregion

        #region 属性

        /// <summary>相机配置编辑行集合，绑定配方上下文视图的可编辑列表。</summary>
        public ObservableCollection<RecipeContextViewDto> RecipeContextViewDto
        {
            get => recipeContextViewShowModels;
            set => SetProperty(ref recipeContextViewShowModels, value);
        }

        #endregion

        #region 命令


        #endregion

        #region 方法

        /// <summary>把全部编辑行还原为 Input 原样交给服务落库；失败仅记录日志，不抛异常。</summary>
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
        /// <summary>重写回车命令：触发保存当前编辑行。</summary>
        public override  void EnterCommandExecute()
        {
             _= SaveAsync();
        }

        /// <summary>经服务查询编辑契约并包装为展示行，整体重建编辑行集合。</summary>
        private async Task LoadAsync()
        {
            var inputs = await cameraConfigurationService.GetCameraNameEditListAsync();
            RecipeContextViewDto = new ObservableCollection<RecipeContextViewDto>(inputs.Select(x => new RecipeContextViewDto(x)));
        }

        #endregion

        #region INavigationAware

        /// <summary>进入页面：加载相机配置列表；失败仅记录日志。</summary>
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

        /// <summary>始终复用当前实例作为导航目标，避免重复创建视图。</summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true; // 复用同一个 View 实例，避免重复创建
        }

        /// <summary>离开页面：不保存，未保存的修改即视为取消。</summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 不保存即离开 = 取消（ADR 0002 规则 3）：Input 随 VM 丢弃，数据库从未被触碰。
        }

        #endregion
    }
}
