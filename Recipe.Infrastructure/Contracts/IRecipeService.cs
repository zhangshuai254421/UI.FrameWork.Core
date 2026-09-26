using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Recipe.Infrastructure.Contracts
{
    /// <summary>
    /// 配方服务契约（ADR 0002 规则 1：实体不出域）。
    /// 只暴露 DTO 投影与显式业务方法；实体级 CRUD 留在
    /// <see cref="Recipe.Infrastructure.RecipeService"/> 的基类里，不进接口。
    /// </summary>
    public interface IRecipeService
    {
        /// <summary>本机配方列表（DTO 投影，过滤空文件夹占位行）。</summary>
        Task<IReadOnlyList<RecipeListItemDto>> GetRecipeListAsync(string machineName, CancellationToken cancellationToken = default);

        /// <summary>本机文件夹（分组）列表：GroupName 去重。</summary>
        Task<IReadOnlyList<string>> GetGroupListAsync(string machineName, CancellationToken cancellationToken = default);

        /// <summary>当前配方 DTO（读 RecipeManager.CurrentRecipeId）；未设置时返回 null。</summary>
        Task<RecipeListItemDto?> GetCurrentRecipeAsync(CancellationToken cancellationToken = default);

        /// <summary>创建文件夹（空文件夹用占位配方行表示）；已存在返回 false。</summary>
        Task<bool> CreateGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default);

        /// <summary>删除文件夹：连同其下全部配方（含占位行）一起删除。</summary>
        Task<bool> DeleteGroupAsync(string groupName, string machineName, CancellationToken cancellationToken = default);

        /// <summary>复制配方：连同参数一起克隆；<paramref name="targetGroupName"/> 为空拷入原文件夹。</summary>
        Task<bool> CopyRecipeAsync(string groupName, string recipeName, string newRecipeName, string machineName, string? targetGroupName = null, CancellationToken cancellationToken = default);

        /// <summary>重命名配方。</summary>
        Task<bool> RenameRecipeAsync(string groupName, string recipeName, string newRecipeName, CancellationToken cancellationToken = default);

        /// <summary>删除配方。</summary>
        Task<bool> DeleteRecipeAsync(string groupName, string recipeName, CancellationToken cancellationToken = default);

        /// <summary>把配方设为当前配方（写 RecipeManager.CurrentRecipeId）。</summary>
        Task<bool> SetCurrentRecipeAsync(int recipeId, CancellationToken cancellationToken = default);
    }
}
