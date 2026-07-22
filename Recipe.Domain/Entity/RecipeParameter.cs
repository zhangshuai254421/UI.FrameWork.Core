using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recipe.Domain
{
    public class RecipeParameter : Entity
    {
        public int RecipeId { get; set; }  
        public virtual Recipe? Recipe { get; set; }

        // 声明一个抽象拷贝方法，要求所有子类实现
    }
}
