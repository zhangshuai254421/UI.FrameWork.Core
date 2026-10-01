using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Recipe.Domain
{
    /// <summary>
    /// 配方实体：由配方名、组名、机器名唯一描述，并携带参数集合。
    /// </summary>
    public class Recipe : Entity
    {
        #region 构造函数

        /// <summary>无参构造，供 ORM/序列化使用。</summary>
        public Recipe()
        {
        }

        /// <summary>构造函数：指定配方 Id、配方名、组名与机器名。</summary>
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

        /// <summary>配方下的参数集合；默认初始化为空集合。</summary>
        public virtual ICollection<RecipeParameter>? Parameters { get; set; } = new HashSet<RecipeParameter>();

        #endregion
    }
}
