using PrismUI.Core;
using Recipe.Infrastructure.Contracts;
using System;
using System.Threading.Tasks;

namespace UI.FrameWork.Core.Main
{
    /// <summary>页头 ViewModel：展示"配方组/配方名"标题，并在切换配方事件到达时自动刷新。</summary>
    public class HeaderViewModel : BindableBase
    {
        #region 字段

        /// <summary>配方服务，用于拉取当前配方信息刷新标题。</summary>
        private readonly IRecipeService _recipeService;
        /// <summary>事件聚合器，订阅切换配方事件。</summary>
        private readonly IEventAggregator _eventAggregator;
        /// <summary>页头标题，初始为默认占位文本。</summary>
        private string _title = "Default Title";

        #endregion

        #region 构造函数

        /// <summary>构造：注入服务并异步拉取标题，同时订阅切换配方事件以触发刷新。</summary>
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

        /// <summary>页头标题，绑定页头视图文本，格式为"配方组/配方名"。</summary>
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
