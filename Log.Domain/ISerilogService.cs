using EFCore.IRepository;
using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Domain
{
    /// <summary>
    /// 日志服务接口：继承通用实体服务，提供日志实体的增删改查与分页能力。
    /// </summary>
    public interface ISerilogService : IEntityServiceBase<SerilogHistory, Guid>
    {
       
    }
}
