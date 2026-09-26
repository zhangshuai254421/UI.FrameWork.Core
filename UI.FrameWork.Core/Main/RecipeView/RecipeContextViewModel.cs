using DryIoc;
using PrismUI.Core;
using Recipe.Domain;
using Recipe.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Linq;

namespace UI.FrameWork.Core.Main.RecipeView
{
    //public class Recipe
    public class RecipeContextViewModel : BaseViewModel, INavigationAware
    {
        #region 字段

        private readonly CameraConfigurationService cameraConfigurationService;
        private readonly IRecipeManagerService recipeManagerService;
        private ObservableCollection<RecipeContextViewDto> recipeContextViewShowModels = new ObservableCollection<RecipeContextViewDto>();

        #endregion

        #region 构造函数

        public RecipeContextViewModel(CameraConfigurationService cameraConfigurationService,IRecipeManagerService recipeManagerService) 
        {
            this.cameraConfigurationService = cameraConfigurationService;
            this.recipeManagerService = recipeManagerService;
        }

        #endregion

        #region 属性

        public ObservableCollection<RecipeContextViewDto> RecipeContextViewDto 
        { get => recipeContextViewShowModels;
            set => SetProperty(ref recipeContextViewShowModels, value);
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

            var result = cameraConfigurationService.GetCurrentRecipeParameterAsync().Result;

            RecipeContextViewDto = new ObservableCollection<RecipeContextViewDto>(result.Select(x => new RecipeContextViewDto { Id = x.Id, CameraName = x.CameraName }));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
           return true; // 复用同一个 View 实例，避免重复创建
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

            var result = cameraConfigurationService.GetCurrentRecipeParameterAsync().Result;




            result.Join(RecipeContextViewDto,
                r => r.Id,              // 从 result 里取 Id
                dto => dto.Id,          // 从 RecipeContextViewDto 里取 Id
                (r, dto) => (Result: r, dto: dto))  // 配对后做点什么
          .ToList()                     // 转成列表
          .ForEach(t => t.Result.CameraName = t.dto.CameraName);  // 逐个更新


            cameraConfigurationService.UpdateRangeAsync(result);

        #endregion

        }
    }
}
