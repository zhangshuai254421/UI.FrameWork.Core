using EFCore.Infrastructure;
using EFCore.Repository;
using Microsoft.EntityFrameworkCore;
using Recipe.Domain;
using Recipe.Infrastructure.Contracts;

namespace Recipe.Infrastructure
{
    public class CameraConfigurationService : RecipeParameterServiceBase<Recipe.Domain.CameraConfiguration, Guid>, ICameraConfigurationService
    {
        public CameraConfigurationService(IUnitOfWork unitofWork, IRecipeManagerService recipeManagerService)
        : base(unitofWork, recipeManagerService)
        {
        }

        /// <summary>
        /// 当前配方下全部相机配置的编辑契约列表：查询投影、无跟踪，不返回实体（ADR 0002）。
        /// </summary>
        public async Task<IReadOnlyList<CameraConfigurationEditInput>> GetCameraNameEditListAsync(CancellationToken cancellationToken = default)
        {
            int currentRecipeId = await GetCurrentRecipeIdAsync(cancellationToken);

            return await _repository.GetQueryable(false)
                .Where(p => p.RecipeId == currentRecipeId)
                .OrderBy(p => p.Id)
                .Select(p => new CameraConfigurationEditInput { Id = p.Id, CameraName = p.CameraName })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 按编辑契约批量保存相机名称：只更新契约里出现的 Id，契约外的行不动。
        /// 跟踪查询加载、赋值后由 UnitOfWork 统一提交。
        /// </summary>
        public async Task<bool> SaveCameraNameEditAsync(IReadOnlyList<CameraConfigurationEditInput> inputs, CancellationToken cancellationToken = default)
        {
            if (inputs.Count == 0)
            {
                return true;
            }

            var nameById = inputs.ToDictionary(x => x.Id, x => x.CameraName);
            var ids = nameById.Keys.ToList();

            return await UpdateRangeAsync(
                p => ids.Contains(p.Id),
                p => p.CameraName = nameById[p.Id],
                cancellationToken);
        }
    }
}
