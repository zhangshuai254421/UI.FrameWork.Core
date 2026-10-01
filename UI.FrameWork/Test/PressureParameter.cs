using EFCore.Repository;
using Framework.Core.CustomAttribute;
using Recipe.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// PressurePlugin 项目 —— 只引用 Recipe.Domain、Recipe.Infrastructure、Framework.Core
[assembly: DefaultDbContext(typeof(Recipe.Infrastructure.DataContext))]   // 一行特性，别无他物
namespace SemiAppliaction.Test
{

    public class PressureParameter : RecipeParameter
    {
        public double TargetPressure { get; set; }
        public double MinTolerance { get; set; }
    }

    public class ComputerParameter : Entity
    {
       public string Name { get; set; } = string.Empty;
    }
}

