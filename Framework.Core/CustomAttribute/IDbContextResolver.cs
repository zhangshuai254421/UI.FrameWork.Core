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
        DbContext Resolve<TEntity>() where TEntity : class;
    }
}
