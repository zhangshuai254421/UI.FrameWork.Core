using EFCore.Infrastructure;
using EFCore.Repository;
using Log.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Infrastructure
{
    public class LogService : EntityServiceBase<Log.Domain.SerilogHistory, Guid>, ILogService
    {
        public LogService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
