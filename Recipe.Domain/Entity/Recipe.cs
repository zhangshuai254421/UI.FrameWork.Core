using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Recipe.Domain
{
    public class Recipe : Entity
    {
        #region 构造函数

        public Recipe()
        {
        }

        public Recipe(int id, string recipeName, string groupName, string machineName)
        {
            base.Id = id;
            RecipeName = recipeName;
            GroupName = groupName;
            MachineName = machineName;
        }

        #endregion

        #region 属性

        /// <summary>
        /// 配方名
        /// </summary>
        public string RecipeName { get; set; }

        /// <summary>
        /// 组名 -如"测试"
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 机器名-如FSD1A
        /// </summary>
        public string MachineName { get; set; }

        public virtual ICollection<RecipeParameter>? Parameters { get; set; } = new HashSet<RecipeParameter>();

        #endregion
    }
}
