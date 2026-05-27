using EFCore.Infrastructure;
using EFCore.Repository;
using Log.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Infrastructure
{
    public class SerilogService : EntityServiceBase<Log.Domain.SerilogHistory, Guid>, ISerilogService
    {
        public SerilogService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }
    }
}
