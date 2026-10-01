using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.Domain
{
    /// <summary>
    /// 配方参数实体基类：记录所属配方，具体参数类型（如相机配置）由此派生。
    /// </summary>
    public class RecipeParameter : Entity
    {
        /// <summary>所属配方的 Id（外键）。</summary>
        public int RecipeId { get; set; }  
        /// <summary>所属配方导航属性，可为空。</summary>
        public virtual Recipe? Recipe { get; set; }

        // 声明一个抽象拷贝方法，要求所有子类实现
    }
}
