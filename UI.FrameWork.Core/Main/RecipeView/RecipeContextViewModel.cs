using PrismUI.Core;
using Recipe.Domain;
using Recipe.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main.RecipeView
{
    public class RecipeContextViewModel : BaseViewModel, INavigationAware
    {
        #region 字段

        private readonly CameraConfigurationService cameraConfigurationService;

        #endregion

        #region 构造函数

        public RecipeContextViewModel(IEventAggregator eventAggregator,CameraConfigurationService cameraConfigurationService) : base(eventAggregator)
        {
            this.cameraConfigurationService = cameraConfigurationService;
        }

        #endregion

        #region 属性

        public ObservableCollection<CameraConfiguration> CameraConfigurations { get; set; } = new ObservableCollection<CameraConfiguration>();

        public CameraConfigurationModel CameraConfiguration { get; set; } = new CameraConfigurationModel();

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
            var result = cameraConfigurationService.GetListAsync().Result;

            CameraConfiguration.CameraName = result.FirstOrDefault().CameraName;
            CameraConfigurations = new ObservableCollection<CameraConfiguration>(result);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
           return true; // 复用同一个 View 实例，避免重复创建
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
        }

        #endregion

        public class CameraConfigurationModel
        {
            public string CameraName { get; set; }
        }
    }
}
