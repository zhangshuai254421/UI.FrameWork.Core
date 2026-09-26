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
        /// 过滤掉空文件夹的占位行（见 <see cref="CreateGroupAsync"/>）。
        /// </summary>
        public async Task<IReadOnlyList<RecipeListItemDto>> GetRecipeListAsync(string machineName, CancellationToken cancellationToken = default)
        {
            return await _repository.GetQueryable(false)
                .Where(p => p.MachineName == machineName && p.RecipeName != string.Empty)
                .OrderBy(p => p.GroupName).ThenBy(p => p.RecipeName)
                .Select(p => new RecipeListItemDto { Id = p.Id, GroupName = p.GroupName, RecipeName = p.RecipeName })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 本机文件夹（分组）列表：GroupName 去重。空文件夹由占位行撑起，因此也会出现。
        /// </summary>
        public async Task<IReadOnlyList<string>> GetGroupListAsync(string machineName, CancellationToken cancellationToken = default)
        {
            return await _repository.GetQueryable(false)
                .Where(p => p.MachineName == machineName)
                .Select(p => p.GroupName)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 创建文件夹：分组并非独立实体，而是配方行的 GroupName；
        /// 空文件夹用一条占位配方（RecipeName 为空串）表示，查询投影会将其过滤。
        /// 文件夹已存在时返回 false。
        /// </summary>
        public async Task<bool> CreateGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default)
        {
            var exists = await _repository.GetQueryable(false)
                .AnyAsync(p => p.MachineName == machineName && p.GroupName == groupName, cancellationToken);
            if (exists)
            {
                return false;
            }

            var marker = new Recipe.Domain.Recipe
            {
                GroupName = groupName,
                RecipeName = string.Empty, // 占位：空串配方名代表文件夹本身，不作为配方展示
                MachineName = machineName,
            };

            var added = await AddAsync(marker, cancellationToken);

            if (added)
            {
                PublishChange(RecipeChangeKind.Added, marker.Id, groupName, string.Empty);
            }

            return added;
        }

        /// <summary>
        /// 删除文件夹：连同其下全部配方（含占位行）一起删除。
        /// </summary>
        public async Task<bool> DeleteGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default)
        {
            var deleted = await DeleteRangeAsync(
                p => p.MachineName == machineName && p.GroupName == groupName, cancellationToken);

            if (deleted)
            {
                PublishChange(RecipeChangeKind.Removed, 0, groupName, string.Empty);
            }

            return deleted;
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
