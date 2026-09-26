using Baksteen.Extensions.DeepCopy;
using EFCore.Infrastructure;
using EFCore.Repository;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using Recipe.Domain;
using Recipe.Infrastructure.Contracts;

namespace Recipe.Infrastructure
{
    /// <summary>
    /// 配方服务（ADR 0002 迁移第③步）：对外只暴露 DTO 与显式业务方法，
    /// 写操作（复制/重命名/删除/应用）完成后发布 <see cref="RecipeChangedEvent"/>，
    /// 列表页订阅刷新，写入方无需自己通知。
    /// </summary>
    public class RecipeService : EntityServiceBase<Recipe.Domain.Recipe, Guid>, IRecipeService
    {
        private readonly IRecipeManagerService _recipeManagerService;
        private readonly IEventAggregator _eventAggregator;

        public RecipeService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService, IEventAggregator eventAggregator)
        : base(unitofWork)
        {
            _recipeManagerService = recipeManagerService;
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 本机配方列表（查询投影、无跟踪，不返回实体）。
        /// </summary>
        public async Task<IReadOnlyList<RecipeListItemDto>> GetRecipeListAsync(string machineName, CancellationToken cancellationToken = default)
        {
            return await _repository.GetQueryable(false)
                .Where(p => p.MachineName == machineName)
                .OrderBy(p => p.GroupName).ThenBy(p => p.RecipeName)
                .Select(p => new RecipeListItemDto { Id = p.Id, GroupName = p.GroupName, RecipeName = p.RecipeName })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 复制配方：连同参数一起克隆（按运行时类型深拷贝，兼容插件派生参数）。
        /// 修复旧 VM 实现无 Include 导致"只拷壳不拷参数"的缺陷。
        /// </summary>
        public async Task<bool> CopyRecipeAsync(string groupName, string recipeName, string newRecipeName, string machineName, CancellationToken cancellationToken = default)
        {
            var source = await _repository.GetQueryable()
                .Include(r => r.Parameters)
                .FirstOrDefaultAsync(r => r.MachineName == machineName
                    && r.GroupName == groupName
                    && r.RecipeName == recipeName, cancellationToken);

            if (source == null)
            {
                return false;
            }

            var copy = new Recipe.Domain.Recipe
            {
                GroupName = source.GroupName,
                RecipeName = newRecipeName,
                MachineName = source.MachineName,
                Parameters = source.Parameters?
                    .Select(p =>
                    {
                        var clone = (RecipeParameter)p.DeepCopy();
                        clone.ResetId();
                        clone.Recipe = null; // 防止深拷贝出的旧图引用被 EF 级联跟踪
                        return clone;
                    })
                    .ToList()
            };

            var added = await AddAsync(copy, cancellationToken);

            if (added)
            {
                PublishChange(RecipeChangeKind.Added, copy.Id, copy.GroupName, copy.RecipeName);
            }

            return added;
        }

        /// <summary>
        /// 重命名配方。
        /// </summary>
        public async Task<bool> RenameRecipeAsync(string groupName, string recipeName, string newRecipeName, CancellationToken cancellationToken = default)
        {
            var updated = await UpdateRangeAsync(
                p => p.GroupName == groupName && p.RecipeName == recipeName,
                p => p.RecipeName = newRecipeName,
                cancellationToken);

            if (updated)
            {
                PublishChange(RecipeChangeKind.Updated, 0, groupName, newRecipeName);
            }

            return updated;
        }

        /// <summary>
        /// 删除配方。
        /// </summary>
        public async Task<bool> DeleteRecipeAsync(string groupName, string recipeName, CancellationToken cancellationToken = default)
        {
            var deleted = await DeleteRangeAsync(p => p.GroupName == groupName && p.RecipeName == recipeName, cancellationToken);

            if (deleted)
            {
                PublishChange(RecipeChangeKind.Removed, 0, groupName, recipeName);
            }

            return deleted;
        }

        /// <summary>
        /// 把配方设为当前配方（写 RecipeManager.CurrentRecipeId）。
        /// </summary>
        public async Task<bool> SetCurrentRecipeAsync(int recipeId, CancellationToken cancellationToken = default)
        {
            return await _recipeManagerService.UpdateRangeAsync(r => true, m => m.CurrentRecipeId = recipeId, cancellationToken);
        }

        private void PublishChange(RecipeChangeKind kind, int recipeId, string groupName, string recipeName)
        {
            _eventAggregator.GetEvent<RecipeChangedEvent>().Publish(new RecipeChangedPayload
            {
                Kind = kind,
                RecipeId = recipeId,
                GroupName = groupName,
                RecipeName = recipeName,
            });
        }
    }
}
