// Copyright (c) 2026 ZhangShuai. All rights reserved.
// 项目：UI.FrameWork —— EFCore.Repository（EF Core 数据访问抽象层）

namespace EFCore.Repository
{
    /// <summary>
    /// 工作单元接口：负责获取各实体的仓储（<see cref="GetRepository{TEntity}"/>）、
    /// 把仓储上累积的变更统一提交到数据库（<see cref="SaveChangeAsync"/>，返回受影响行数），
    /// 以及开启显式事务（<see cref="BeginTransactionAsync"/>，返回 <see cref="IUnitOfTransaction"/>）。
    /// <para>典型用法：通过仓储做出一组相关修改后，调用 <see cref="SaveChangeAsync"/> 一次性提交。</para>
    /// </summary>
    public interface IUnitOfWork
    {
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
        Task<int> SaveChangeAsync(CancellationToken cancellationToken = default);
        Task<IUnitOfTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
