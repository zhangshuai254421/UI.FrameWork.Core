using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
[assembly: DefaultDbContext(typeof(DataContext))]
namespace Recipe.Domain
{
    public class Recipe:Entity
    {

        /// <summary>
        /// 配方名
        /// </summary>
        public string RecipeName { get; set; }

        /// <summary>
        /// 组名 -如“测试”
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 机器名-如FSD1A
        /// </summary>
        public string MachineName { get; set; }

        public Recipe(int id, string recipeName, string groupName, string machineName)
        {
            base.Id = id;
            RecipeName = recipeName;
            GroupName = groupName;
            MachineName = machineName;
        }

        public virtual ICollection<RecipeParameter>? Parameters { get; set; } = new HashSet<RecipeParameter>();
    }
}
