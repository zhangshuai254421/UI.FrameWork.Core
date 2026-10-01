using EFCore.Infrastructure;
using EFCore.Repository;
using Log.Domain;
using Log.Infrastructure.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log.Infrastructure
{
    /// <summary>
    /// 日志服务（ADR 0002 迁移第④步）：对外提供 DTO 投影的分页查询，实体不出数据层。
    /// </summary>
    public class SerilogService : EntityServiceBase<SerilogHistory, Guid>, ISerilogService
    {
        public SerilogService(IUnitOfWork unitofWork) : base(unitofWork)
        {
        }

        /// <summary>
        /// 分页查询日志（DTO 投影、无跟踪）。起止日期均可选，仅两者同时给出时生效。
        /// </summary>
        public async Task<PagedResult<LogEntryDto>> GetLogPageAsync(
            PageParameter parameter, DateTime? startDate = null, DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _repository.GetQueryable(false);

            if (startDate.HasValue && endDate.HasValue)
            {
                // Serilog Timestamp 为字符串格式，天然支持字典序比较（与旧实现一致）
                var start = startDate.Value.ToString("yyyy-MM-dd");
                var end = endDate.Value.AddDays(1).ToString("yyyy-MM-dd");

                query = query.Where(x => string.Compare(x.Timestamp, start) >= 0
                                      && string.Compare(x.Timestamp, end) < 0);
            }

            return new PagedResult<LogEntryDto>
            {
                Total = await query.CountAsync(cancellationToken),
                Data = await query
                    .OrderBy(x => x.Id) // 显式排序，保证分页稳定（旧实现依赖 SQLite rowid 顺序）
                    .Skip((parameter.PageNum - 1) * parameter.PageSize)
                    .Take(parameter.PageSize)
                    .Select(x => new LogEntryDto
                    {
                        Id = x.Id,
                        Timestamp = x.Timestamp,
                        Level = x.Level,
                        RenderedMessage = x.RenderedMessage
                    })
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
