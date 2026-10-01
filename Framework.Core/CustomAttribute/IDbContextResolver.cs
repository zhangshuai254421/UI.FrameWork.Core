using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Core.CustomAttribute
{
    // 解析器接口
    public interface IDbContextResolver
    {
        /// <summary>解析实体类型对应的 DbContext 实例；无法解析时抛出异常。</summary>
        DbContext Resolve<TEntity>() where TEntity : class;
    }
}
