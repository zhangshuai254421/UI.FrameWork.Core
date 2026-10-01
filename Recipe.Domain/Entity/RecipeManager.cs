using EFCore.Repository;

namespace Recipe.Domain
{
    /// <summary>
    /// 配方管理器实体：单例式记录当前选中的配方（以 Id 引用）。
    /// </summary>
    public class RecipeManager : Entity
    {
        #region 构造函数

        /// <summary>无参构造，供 ORM/序列化使用。</summary>
        public RecipeManager()
        {
        }

        /// <summary>构造函数：指定管理器 Id 与当前配方 Id。</summary>
        public RecipeManager(int id, int currentRecipeId)
        {
            base.Id = id;
            CurrentRecipeId = currentRecipeId;
        }

        #endregion

        #region 属性

        public int CurrentRecipeId { get; set; }  // 存 Id，而不是整个对象

        public Recipe? CurrentRecipe { get; set; } // 导航属性（可选）

        #endregion
    }
}
