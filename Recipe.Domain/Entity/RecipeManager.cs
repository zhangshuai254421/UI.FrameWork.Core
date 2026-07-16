using EFCore.Repository;

namespace Recipe.Domain
{
    public class RecipeManager : Entity
    {
        #region 构造函数

        public RecipeManager()
        {
        }

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
