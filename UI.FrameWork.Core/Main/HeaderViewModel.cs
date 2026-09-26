using PrismUI.Core;
using Recipe.Infrastructure.Contracts;
using System;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    public class HeaderViewModel : BindableBase
    {
        #region 字段

        private readonly IRecipeService _recipeService;
        private readonly IEventAggregator _eventAggregator;
        private string _title = "Default Title";

        #endregion

        #region 构造函数

        public HeaderViewModel(IRecipeService recipeService, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _recipeService = recipeService;
            _ = UpdateTitleAsync();

            _eventAggregator.GetEvent<ChangeRecipeEvent>().Subscribe((recipeName) =>
            {
                _ = UpdateTitleAsync();
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

        /// <summary>
        /// 从 service 拉当前配方 DTO 刷新标题（ADR 0002：VM 不碰实体、不同步阻塞）。
        /// 标题是纯展示，加载失败时保持现状即可。
        /// </summary>
        private async Task UpdateTitleAsync()
        {
            try
            {
                var current = await _recipeService.GetCurrentRecipeAsync();
                Title = $"{current?.GroupName}/{current?.RecipeName}";
            }
            catch (Exception)
            {
                // 保持现状
            }
        }

        #endregion
    }
}
