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

    /// <summary>压力测试参数：在配方参数基础上增加目标压力与容差字段。</summary>
    public class PressureParameter : RecipeParameter
    {
        /// <summary>目标压力值。</summary>
        public double TargetPressure { get; set; }
        /// <summary>压力容差下限，低于目标压力的允许偏差。</summary>
        public double MinTolerance { get; set; }
    }

    /// <summary>测试用简单实体：仅包含名称的计算机参数。</summary>
    public class ComputerParameter : Entity
    {
       /// <summary>参数名称。</summary>
       public string Name { get; set; } = string.Empty;
    }
}

