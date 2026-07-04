using PrismUI.Core;
using Recipe.Domain;

namespace UI.FrameWork.Core.Main
{
    public class HeaderViewModel : BindableBase
    {
        #region 字段

        private readonly IRecipeManagerService _recipeManagerService;
        private readonly IEventAggregator _eventAggregator;
        private string _title = "Default Title";

        #endregion

        #region 构造函数

        public HeaderViewModel(IRecipeManagerService recipeManagerService, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _recipeManagerService = recipeManagerService;
            UpdateTitle();

            _eventAggregator.GetEvent<ChangeRecipeEvent>().Subscribe((recipeName) =>
            {
                UpdateTitle();
            });
        }

        #endregion

        #region 属性

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        #endregion

        #region 方法

        public void UpdateTitle()
        {
            Title = $"{_recipeManagerService.GetListAsync().Result.FirstOrDefault()?.CurrentRecipe.GroupName}/" +
                    $"{_recipeManagerService.GetListAsync().Result.FirstOrDefault()?.CurrentRecipe.RecipeName}";
        }

        #endregion
    }
}
