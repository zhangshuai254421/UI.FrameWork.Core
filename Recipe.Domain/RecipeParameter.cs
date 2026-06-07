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
        public virtual Recipe? Recipe { get; set; }
    }
}
