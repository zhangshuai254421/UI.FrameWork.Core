using EFCore.IRepository;
using EFCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Domain
{
    public interface ILogService : IEntityServiceBase<SerilogHistory, Guid>
    {
       
    }
}
